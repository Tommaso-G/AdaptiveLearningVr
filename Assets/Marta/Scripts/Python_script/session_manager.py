from dataclasses import dataclass, field
from enum import Enum
from datetime import datetime
from typing import Dict, List, Optional, Any
import json


from adaptive_fire_training_bn import (
    AdaptiveTrainingManager,
    ChapterConfig,
    AdaptiveDecision,
    ChapterState,
    SkillLevel,
    InitialActivationPolicy
)


# ---------------------------------------------------------------------------
# Enumerazioni e Strutture Dati
# ---------------------------------------------------------------------------

# in che fase si trova l'iterazione corrente
class IterationState(Enum):
    STARTED = "started"
    IN_PROGRESS = "in_progress"
    COMPLETE = "complete"
    INTERRUPTED = "interrupted"


# classe per con info sull'iterazione corrente
@dataclass
class IterationData:
    """Dati di una singola iterazione."""
    iteration_number: int
    state: IterationState = IterationState.STARTED
    started_at: datetime = field(default_factory=datetime.now)
    completed_at: Optional[datetime] = None
    chapters_completed: List[str] = field(default_factory=list)
    chapters_removed: List[str] = field(default_factory=list)
    chapter_decisions: Dict[str, Any] = field(default_factory=dict)
    
    # Salva le prior di tutti i capitoli, anche non attivi per l'iterazione corrente
    def save_initial_chapters_prior(self, chapter_id: str, initialState: Optional[ChapterState] = None, prevState: Optional[Dict[str, Any]] = None):
        """Salva il valore iniziale delle prior, all'inizio della nuova iterazione"""
        
        if initialState == None:
            self.chapter_decisions[chapter_id] = {
                "posterior": prevState["posterior"],
                "feedback_level": prevState["feedback_level"],
                "difficulty_level": prevState["difficulty_level"],
                "message": prevState["message"]
            }
        else:
            # Storica la decisione
            self.chapter_decisions[chapter_id] = {
                "posterior": initialState.skill_posterior,
                "feedback_level": initialState.feedback_level,
                "difficulty_level": initialState.difficulty_version,
                "message": "prima inizializzazione"
            }
    
    # aggiorna le prior per il capitolo appena visto 
    def mark_chapter_completed(self, chapter_id: str, decision: AdaptiveDecision):
        """Marca un capitolo come completato in questa iterazione."""
        if chapter_id not in self.chapters_completed:
            self.chapters_completed.append(chapter_id)
        
        # Storica la decisione
        self.chapter_decisions[chapter_id] = {
            "posterior": decision.skill_posterior,
            "feedback_level": decision.new_feedback_level,
            "difficulty_level": decision.new_difficulty_level,
            "message": decision.message,
        }
    
    def mark_chapter_removed(self, chapter_id: str):
        """Marca un capitolo come rimosso (early remove o remove normale)."""
        self.chapters_removed.append(chapter_id)

    def is_complete(self, all_active_chapters: List[str]) -> bool:
        """
        Iterazione completa se tutti i capitoli attivi
        sono stati o completati o rimossi.
        """
        effective_done = set(self.chapters_completed) | set(self.chapters_removed)
        return effective_done == set(all_active_chapters)
    
    def to_dict(self) -> Dict:
        """Serializza a dict per JSON."""
        return {
            "iteration_number": self.iteration_number,
            "state": self.state.value,
            "started_at": self.started_at.isoformat(),
            "completed_at": self.completed_at.isoformat() if self.completed_at else None,
            "chapters_completed": self.chapters_completed,
            "chapter_decisions": self.chapter_decisions,
        }


# ---------------------------------------------------------------------------
# Session Manager Principale
# ---------------------------------------------------------------------------

class AdaptiveSessionManager:
    """
    Gestore della sessione adattiva.
    
    Wrappa AdaptiveTrainingManager e aggiunge:
    - Tracciamento stato iterazione
    - Rollback per iterazioni incomplete
    - Persistenza dati sessione
    """
    
    def __init__(self, session_id: str):
        """
        Inizializza una nuova sessione.
        
        Args:
            session_id: ID univoco della sessione (es: "user_john_20240115_100000")
        """
        self.session_id = session_id
        self.training_manager: Optional[AdaptiveTrainingManager] = None
        
        # Tracciamento iterazioni
        self.iterations: Dict[int, IterationData] = {}
        self.current_iteration_number = 1
        self.last_complete_iteration = 0
        
        print(f"[SESSION] Creata nuova sessione: {session_id}")
    
    def initialize(self, chapter_configs: List[ChapterConfig]):
        """
        Inizializza il training manager con i capitoli.
        
        Args:
            chapter_configs: Lista di configurazioni capitoli
        """
        self.training_manager = AdaptiveTrainingManager(chapter_configs, initial_policy=InitialActivationPolicy.INTERMEDIATE)
        print(f"[SESSION] Inizializzato with {len(chapter_configs)} capitoli")
    
    def start_new_iteration(self) -> Dict:
        """
        Avvia una nuova iterazione.
        
        Se l'iterazione precedente era incompleta (IN_PROGRESS),
        fa il rollback automaticamente.
        
        Ritorna:
            Dict con stato della sessione
        """
        
        # Controlla stato dell'iterazione precedente
        if self.current_iteration_number > 1:
            previous_iter_number = self.current_iteration_number - 1
            previous_iter = self.iterations.get(previous_iter_number)

            if self.last_complete_iteration > 0:
                last_complete_iter = self.iterations[self.last_complete_iteration]
            
            print(f"previuous iter state: [{previous_iter.state}] and number: [{previous_iter.iteration_number}]")

            if previous_iter and previous_iter.state == IterationState.IN_PROGRESS:
                print(
                    f"[SESSION] Iterazione {previous_iter_number} era incompleta. "
                    f"ROLLBACK all'iterazione {self.last_complete_iteration}"
                )
                
                # ===== ROLLBACK =====
                # 1. Scarta l'iterazione incompleta
                del self.iterations[previous_iter_number]
                
                # 2. Ripristina BN alla posterior dell'ultima iterazione completa:
                # Ripristina la posterior di ogni capitolo
                for chapter_id, chapter_state in self.training_manager.chapter_states.items():
                    if chapter_id in last_complete_iter.chapter_decisions:
                        decision_data = last_complete_iter.chapter_decisions[chapter_id]
                        # Ripristina la posterior (diventa prior per la prossima inferenza)
                        chapter_state.skill_posterior = decision_data["posterior"]
                        ##print(f"(Capitolo: {chapter_id}, current prior -> {chapter_state.skill_posterior})")
                
                print(
                    f"[SESSION] BN ripristinato alla posterior di "
                    f"iterazione {self.last_complete_iteration}"
                )
                
                # 3. Non incrementare current_iteration_number (ricomincia da capo)
                self.current_iteration_number = self.last_complete_iteration + 1
        
        # ===== CREA NUOVA ITERAZIONE =====
        new_iter = IterationData(iteration_number=self.current_iteration_number)
        self.iterations[self.current_iteration_number] = new_iter

        if self.current_iteration_number > 1:
            for chapter_id, chapter_state in self.training_manager.chapter_states.items():
                decision_data = last_complete_iter.chapter_decisions[chapter_id]
                new_iter.save_initial_chapters_prior(chapter_id=chapter_id, prevState=decision_data)
                print(f"(Capitolo: {chapter_id}, current prior -> {chapter_state.skill_posterior})")
        else:
            for chapter_id, chapter_state in self.training_manager.chapter_states.items():
                new_iter.save_initial_chapters_prior(chapter_id=chapter_id, initialState=chapter_state)
                print(f"(Capitolo: {chapter_id}, current prior -> {chapter_state.skill_posterior})")

                    
        
        print(f"[SESSION] Nuova iterazione {self.current_iteration_number} avviata")
        
        return {
            "session_id": self.session_id,
            "iteration_number": self.current_iteration_number,
            "last_complete_iteration": self.last_complete_iteration,
            "state": "ready",
        }
    
    def observe_chapter(
        self,
        chapter_id: str,
        chapter_name: str,
        errors: int,
        time_sec: float
    ) -> AdaptiveDecision:
        """
        Osserva il completamento di un capitolo.
        
        Args:
            chapter_id: ID del capitolo
            errors: Numero di errori commessi
            time_sec: Tempo impiegato in secondi
        
        Returns:
            AdaptiveDecision con skill posterior e feedback
        """
        
        if self.training_manager is None:
            raise RuntimeError("Session non inizializzata. Chiama initialize() prima.")
        
        print(self.current_iteration_number)

        current_iter = self.iterations[self.current_iteration_number]
        
        # Aggiorna stato iterazione
        current_iter.state = IterationState.IN_PROGRESS
        
        # Chiama il BN (il tuo training_manager)
        decision = self.training_manager.observe(chapter_id, chapter_name, errors, time_sec, self.current_iteration_number)
        
        # Registra il completamento
        current_iter.mark_chapter_completed(chapter_id, decision)

        # Registra un capiotlo rimosso con early_remove (se presente)
        if decision.remove_optional and decision.removed_chapter_id:
            current_iter.mark_chapter_removed(decision.removed_chapter_id)
        
        chapters_done = len(current_iter.chapters_completed)
        print(
            f"[SESSION] Iterazione {self.current_iteration_number}: "
            f"{chapter_id} completato ({chapters_done} capitoli)"
        )
        
        # Aggiungi info di iterazione alla decisione (opzionale, per Unity)
        if not hasattr(decision, 'iteration_number'):
            decision.iteration_number = self.current_iteration_number
        
        return decision
    
    def end_iteration(self, active_chapters: List[str]) -> Dict:
        """
        Marca la fine dell'iterazione.
        
        Deve essere chiamato dal programmatore PRIMA di fermare Unity.
        Controlla se tutti i capitoli attivi sono stati completati.
        
        Se sì:
        - Marca iterazione come COMPLETE
        - Aggiorna last_complete_iteration
        - Incrementa current_iteration_number (prossima iterazione avrà numero+1)
        
        Se no:
        - Iterazione rimane IN_PROGRESS
        - Al prossimo resume, verrà fatto rollback
        
        Args:
            active_chapters: Lista dei capitoli attivi in questa iterazione
        
        Returns:
            Dict con risultato
        """
        print(f"[SESSION] end_iteration chiamato. current_iter={self.current_iteration_number}, "
        f"iterations_keys={list(self.iterations.keys())}, "
        f"active_chapters={active_chapters}")

        # Guard: se l'iterazione corrente non esiste, è già stata chiusa
        if self.current_iteration_number not in self.iterations:
            print(f"[SESSION] WARN: end_iteration chiamato ma iterazione "
                f"{self.current_iteration_number} non esiste. Ignorato.")
            return {
                "status": "already_ended",
                "iteration_number": self.last_complete_iteration,
                "next_iteration": self.current_iteration_number,
                "incomplete_chapters": [],
            }

        # Guard: se già completata, non processare di nuovo
        current_iter = self.iterations[self.current_iteration_number]
        if current_iter.state == IterationState.COMPLETE:
            print(f"[SESSION] WARN: end_iteration chiamato su iterazione già COMPLETE. Ignorato.")
            return {
                "status": "already_ended",
                "iteration_number": self.last_complete_iteration,
                "next_iteration": self.current_iteration_number,
                "incomplete_chapters": [],
            }
        
        # Controlla completamento
        if current_iter.is_complete(active_chapters):
            current_iter.state = IterationState.COMPLETE
            current_iter.completed_at = datetime.now()
            self.last_complete_iteration = self.current_iteration_number
            
            duration = (current_iter.completed_at - current_iter.started_at).total_seconds()
            
            print(
                f"[SESSION] ✓ Iterazione {self.current_iteration_number} COMPLETA! "
                f"(durata: {duration:.1f}s)"
                f"(capitoli completati: {current_iter.chapters_completed})"
            )
            
            # Prepara la prossima iterazione
            self.current_iteration_number += 1
            
            return {
                "status": "iteration_complete",
                "iteration_number": self.last_complete_iteration,
                "next_iteration": self.current_iteration_number,
                "duration_seconds": duration,
                "completed_chapters": current_iter.chapters_completed,
                "incomplete_chapters": [],
            }
        else:
            incomplete_chapters = [
                c for c in active_chapters
                if c not in current_iter.chapters_completed
            ]
            
            print(
                f"[SESSION] Iterazione {self.current_iteration_number} INCOMPLETA. "
                f"Capitoli mancanti: {incomplete_chapters}"
            )

            self.current_iteration_number += 1
            
            # Iterazione rimane IN_PROGRESS
            # Al prossimo resume(), verrà fatto rollback automaticamente
            
            return {
                "status": "iteration_incomplete",
                "completed_chapters": current_iter.chapters_completed,
                "incomplete_chapters": incomplete_chapters,
                "note": "Se chiudi Unity ora, al prossimo play verrà fatto rollback automatico",
            }
    
    def get_iteration_status(self) -> Dict:
        """
        Restituisce lo stato attuale della sessione.
        
        Utile per debug e per verificare se è necessario rollback.
        
        Returns:
            Dict con stato corrente
        """
        current_iter = self.iterations.get(self.current_iteration_number)
        
        return {
            "session_id": self.session_id,
            "current_iteration": self.current_iteration_number,
            "current_iteration_state": current_iter.state.value if current_iter else None,
            "last_complete_iteration": self.last_complete_iteration,
            "chapters_completed_this_iteration": (
                current_iter.chapters_completed if current_iter else []
            ),
        }
    
    def get_session_data(self) -> Dict:
        """
        Esporta tutti i dati della sessione.
        
        Utile per debug, salvataggio su database, analytics.
        
        Returns:
            Dict con tutti i dati della sessione
        """
        return {
            "session_id": self.session_id,
            "created_at": datetime.now().isoformat(),
            "current_iteration": self.current_iteration_number,
            "last_complete_iteration": self.last_complete_iteration,
            "iterations": {
                iter_num: iter_data.to_dict()
                for iter_num, iter_data in self.iterations.items()
            },
        }
    
    def export_to_json(self, filename: str = None) -> str:
        """
        Esporta la sessione a JSON.
        
        Args:
            filename: Se fornito, scrive su file. Altrimenti ritorna string.
        
        Returns:
            String JSON
        """
        data = self.get_session_data()
        json_str = json.dumps(data, indent=2, default=str)
        
        if filename:
            with open(filename, 'w') as f:
                f.write(json_str)
            print(f"[SESSION] Sessione esportata in {filename}")
        
        return json_str
    
@classmethod
def import_from_json(cls, filename: str) -> "AdaptiveSessionManager":
    """
    Ripristina una sessione da un file JSON salvato da export_to_json.
    Ricostruisce iterations e i posterior dei capitoli,
    ma NON ricrea il training_manager (serve una StartSession da Unity).
    """
    with open(filename, 'r') as f:
        data = json.load(f)

    session_id = data["session_id"]
    mgr = cls(session_id)
    mgr.current_iteration_number = data["current_iteration"]
    mgr.last_complete_iteration = data["last_complete_iteration"]

    for iter_num_str, iter_dict in data.get("iterations", {}).items():
        iter_num = int(iter_num_str)
        iter_data = IterationData(
            iteration_number=iter_num,
            state=IterationState(iter_dict["state"]),
            chapters_completed=iter_dict.get("chapters_completed", []),
            chapter_decisions=iter_dict.get("chapter_decisions", {}),
        )
        if iter_dict.get("completed_at"):
            iter_data.completed_at = datetime.fromisoformat(iter_dict["completed_at"])
        mgr.iterations[iter_num] = iter_data

    print(f"[SESSION] Ripristinata da disco: {session_id}, "
          f"iter={mgr.current_iteration_number}, "
          f"last_complete={mgr.last_complete_iteration}")
    return mgr

def print_debug_info(self):
    """Stampa info di debug della sessione."""
    print(f"\n=== [SESSION DEBUG] {self.session_id} ===")
    print(f"Current iteration: {self.current_iteration_number}")
    print(f"Last complete iteration: {self.last_complete_iteration}")
    print(f"\nIterations:")
    
    for iter_num, iter_data in self.iterations.items():
        state_emoji = {
            IterationState.STARTED: "🔄",
            IterationState.IN_PROGRESS: "⏳",
            IterationState.COMPLETE: "✓",
            IterationState.INTERRUPTED: "❌",
        }.get(iter_data.state, "?")
        
        print(
            f"  Iteration {iter_num}: {state_emoji} {iter_data.state.value} "
            f"({len(iter_data.chapters_completed)} capitoli)"
        )