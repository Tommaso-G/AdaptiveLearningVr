# server.py
# pip install fastapi uvicorn

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Dict, List, Optional
import uvicorn
import os

from adaptive_fire_training_bn import (
    ChapterConfig,
    OptionalStatus,
)

# ===== NUOVO: Importa SessionManager =====
from session_manager import AdaptiveSessionManager, IterationState

app = FastAPI(title="Adaptive Fire Training API")

# ── Stato globale delle sessioni ─────────────────────────────────────────
# In produzione usa un database o Redis. Per un singolo utente alla volta
# un dizionario in memoria è sufficiente.
# Chiave: session_id (stringa), Valore: AdaptiveSessionManager
sessions: Dict[str, AdaptiveSessionManager] = {}

SESSIONS_DIR = "./sessions_data"
if not os.path.exists(SESSIONS_DIR):
    os.makedirs(SESSIONS_DIR)

# ── Modelli di richiesta e risposta ──────────────────────────────────────

class StartSessionRequest(BaseModel):
    session_id: str
    chapters: List[dict]  # lista di configurazioni capitoli da Unity
    reset_all: bool = True  # Se True: nuova sessione, se False: riprendi

class ObserveRequest(BaseModel):
    session_id: str
    chapter_id: str
    chapter_name: str
    errors: int
    time_sec: float

class EndIterationRequest(BaseModel):
    session_id: str
    active_chapters: List[str]

class DecisionResponse(BaseModel):
    chapter_id: str
    skill_label: str
    posterior_expert: float
    posterior_intermediate: float
    posterior_novice: float
    feedback_level: int        # 0=nessuno, 1=highlight, 2=highlight+istruzioni
    difficulty_level: int
    feedback_changed: bool
    difficulty_changed: bool
    add_optional: bool
    added_chapter_id: Optional[str] = None
    removed_chapter_id: Optional[str] = None  
    remove_optional: bool
    chapter_mastered: bool
    active_chapters: List[str]
    message: str
    iteration_number: int
    iteration_status: str  # Stato iterazione (complete, incomplete, in_progress)

class ChapterDetail(BaseModel):
    chapter_id: str
    is_active: bool
    feedback_level: int
    difficulty_level: int
    chapter_prior: List[float]

class SessionStateResponse(BaseModel):
    session_id: str
    active_chapters: List[str]          # quali capitoli attivare in VRBuilder
    chapter_details: List[ChapterDetail]   # dettagli per ogni capitolo
    is_complete: bool                   # tutti gli opzionali padroneggiati?

# ── Helper functions ─────────────────────────────────────────────────────
 
def save_session_to_json(session_id: str, session_mgr: AdaptiveSessionManager):
    """Salva la sessione a JSON (opzionale, per backup)."""
    try:
        filepath = os.path.join(SESSIONS_DIR, f"{session_id}.json")
        session_mgr.export_to_json(filepath)
        print(f"[SAVE] Sessione salvata in {filepath}")
    except Exception as e:
        print(f"[WARN] Impossibile salvare sessione: {e}")

# ── Endpoint: avvia una nuova sessione ───────────────────────────────────

@app.post("/session/start")
def start_session(request: StartSessionRequest):
    """
    Crea un nuovo AdaptiveTrainingManager per questa sessione.
    Chiamato da Unity all'avvio dell'esperienza.

    Body JSON atteso:
    {
        "session_id": "player_001",
        "chapters": [
            {"chapter_id": "cap1", "name": "Allarme", "is_mandatory": true},
            {"chapter_id": "cap4_opz", "name": "Estintore", "is_mandatory": false,
            "max_possible_errors: "# errors","min_expected_time_sec": "min_time", "max_expected_time_sec": "max_time" }
        ]
    }
    """
    if request.reset_all:
        # ===== NUOVA SESSIONE =====
        if request.session_id in sessions:
            print(f"[SESSION] Sovrascrivendo sessione esistente: {request.session_id}")
            # Salva la vecchia sessione prima di eliminarla
            save_session_to_json(request.session_id, sessions[request.session_id])
            del sessions[request.session_id]

        configs = []    
        for c in request.chapters:
            configs.append(ChapterConfig(
                chapter_id=c["chapter_id"],
                name=c.get("name", c["chapter_id"]),
                is_mandatory=c["is_mandatory"],
                weight=c.get("weight", 1.0),
                max_iterations=c.get("max_iterations", 5),
                has_difficulty_level=c.get("has_difficulty_level"),
                max_possible_errors=c.get("max_errors"),
                min_expected_time_sec=c.get("min_time_sec"),
                max_expected_time_sec=c.get("max_time_sec")
            ))

        # Crea il SessionManager
        session_mgr = AdaptiveSessionManager(request.session_id)
        session_mgr.initialize(configs)
        session_mgr.start_new_iteration()
        sessions[request.session_id] = session_mgr
        
        print(f"[SESSION] Nuova sessione creata: {request.session_id}")
        
        return {
            "status": "session_created",
            "session_id": request.session_id,
            "iteration_number": session_mgr.current_iteration_number,
            "active_chapters": session_mgr.training_manager.get_active_chapters(),
        }
    
    else:
        # ===== RIPRENDI SESSIONE ESISTENTE =====
        if request.session_id not in sessions:
            raise HTTPException(
                status_code=404,
                detail=f"Sessione '{request.session_id}' non trovata. "
                       f"Chiama con reset_all=true prima."
            )
        
        session_mgr = sessions[request.session_id]
        
        # ===== NUOVO: Avvia nuova iterazione (con rollback automatico se necessario) =====
        iteration_result = session_mgr.start_new_iteration()
        
        print(f"[SESSION] Sessione ripresa: {request.session_id}, "
              f"iterazione {iteration_result['iteration_number']}")
        
        return {
            "status": "session_resumed",
            "session_id": request.session_id,
            "iteration_number": iteration_result['iteration_number'],
            "last_complete_iteration": iteration_result['last_complete_iteration'],
            "active_chapters": session_mgr.training_manager.get_active_chapters(),
            "note": "Se iterazione precedente era incompleta, rollback automatico applicato"
        }

# ── Endpoint: recupera dati sessione precedente per inizializzazione ──────────────────────
@app.get("/session/{session_id}/state")
def get_session_state(session_id: str):
    if session_id not in sessions:
        raise HTTPException(status_code=404,
                            detail="Sessione non trovata.")

    session_mgr = sessions[session_id]
    manager = session_mgr.training_manager
    
    # Stato iterazione
    iter_status = session_mgr.get_iteration_status()

    # Feedback corrente per ogni capitolo attivo
    chapter_feedback = {
        cid: state.feedback_level
        for cid, state in manager.chapter_states.items()
        if state.is_active
    }

    # Verifica completamento
    has_available = any(
        state.optional_status in (
            OptionalStatus.NEVER_SHOWN,
            OptionalStatus.REMOVED
        )
        for cid, state in manager.chapter_states.items()
        if cid in manager.optional_ids
    )

    is_complete = not has_available and not any(
        state.is_active and cid in manager.optional_ids
        for cid, state in manager.chapter_states.items()
    )

    chapter_details = [
        ChapterDetail(
            chapter_id=cid,
            is_active=state.is_active,
            feedback_level=state.feedback_level,
            difficulty_level=state.difficulty_version,
            chapter_prior=state.skill_posterior
        )
        for cid, state in manager.chapter_states.items()
    ]

    return {
        "session_id": session_id,
        "active_chapters": manager.get_active_chapters(),
        "chapter_details": chapter_details,
        "is_complete": is_complete,
        "iteration_number":session_mgr.current_iteration_number,
        "last_complete_iteration":session_mgr.last_complete_iteration,
        "iteration_state":iter_status['current_iteration_state'] or "started"
    }

# ── Endpoint: invia osservazione e ricevi decisione ──────────────────────

@app.post("/observe", response_model=DecisionResponse)
def observe(request: ObserveRequest):
    """
    Riceve errori e tempo per un capitolo, aggiorna la BN,
    restituisce la decisione adattiva.

    Chiamato da Unity ogni volta che l'utente completa un capitolo.

    Body JSON atteso:
    {
        "session_id": "player_001",
        "chapter_id": "cap1",
        "errors": 3,
        "time_sec": 8.5
    }
    """
    if request.session_id not in sessions:
        raise HTTPException(
            status_code=404,
            detail=f"Sessione '{request.session_id}' non trovata. "
                   f"Chiama /session/start prima."
        )

    session_mgr = sessions[request.session_id]
    manager = session_mgr.training_manager

    # Verifica che il capitolo esista
    if request.chapter_id not in manager.configs:
        raise HTTPException(
            status_code=400,
            detail=f"Capitolo '{request.chapter_id}' non trovato nella sessione."
        )

    decision = session_mgr.observe_chapter(
        chapter_id=request.chapter_id,
        chapter_name=request.chapter_name,
        errors=request.errors,
        time_sec=request.time_sec
    )

    # Ricava i dati dal state del capitolo (feedback e difficoltà possono essere cambiati)
    state = manager.chapter_states[request.chapter_id]

    return DecisionResponse(
        chapter_id=decision.chapter_id,
        skill_label=decision.skill_label,
        posterior_expert=round(decision.skill_posterior[0], 4),
        posterior_intermediate=round(decision.skill_posterior[1], 4),
        posterior_novice=round(decision.skill_posterior[2], 4),
        feedback_level=decision.new_feedback_level,
        difficulty_level=decision.new_difficulty_level,
        feedback_changed=decision.feedback_changed,
        difficulty_changed=decision.difficulty_changed,
        add_optional=decision.add_optional,
        added_chapter_id = decision.added_chapter_id,
        removed_chapter_id = decision.removed_chapter_id,
        remove_optional=decision.remove_optional,
        chapter_mastered=decision.chapter_mastered,
        active_chapters=manager.get_active_chapters(),
        message=decision.message,
        iteration_number=session_mgr.current_iteration_number,
        iteration_status="in_progress"
    )

# ── Endpoint: Fine iterazione ────────────────────────────────────
 
@app.post("/session/{session_id}/end_iteration")
def end_iteration(session_id: str, request: EndIterationRequest):
    """
    Marca la fine dell'iterazione.
    
    Deve essere chiamato dal programmatore PRIMA di fermare Unity.
    Controlla se tutti i capitoli attivi sono stati completati.
    
    Se sì:
    - Marca iterazione come COMPLETE
    - Incrementa il numero iterazione (prossima sarà +1)
    
    Se no:
    - Iterazione rimane IN_PROGRESS
    - Al prossimo play (reset_all=false), il rollback accade automaticamente
    
    Body JSON atteso:
    {
        "session_id": "player_001",
        "active_chapters": ["cap1", "cap2", "cap3"]
    }
    """
    if session_id not in sessions:
        raise HTTPException(status_code=404, detail="Sessione non trovata.")
    
    session_mgr = sessions[session_id]
    result = session_mgr.end_iteration(request.active_chapters)
    
    # Salva la sessione dopo la fine dell'iterazione (opzionale)
    save_session_to_json(session_id, session_mgr)
    
    return {
        "status": result['status'],
        "iteration_number": result.get('iteration_number'),
        "next_iteration": result.get('next_iteration'),
        "incomplete_chapters": result.get('incomplete_chapters', []),
    }

# ── Endpoint: stato corrente della sessione ──────────────────────────────

@app.get("/session/{session_id}/summary")
def get_summary(session_id: str):
    """
    Restituisce il riepilogo completo della sessione.
    Utile per debug o per mostrare statistiche all'utente.
    """
    if session_id not in sessions:
        raise HTTPException(status_code=404, detail="Sessione non trovata.")
    
    session_mgr = sessions[session_id]
    manager = session_mgr.training_manager
    
    return {
        "session_id": session_id,
        "iteration_number": session_mgr.current_iteration_number,
        "last_complete_iteration": session_mgr.last_complete_iteration,
        "chapter_summary": manager.get_chapter_summary(),
        "session_data": session_mgr.get_session_data()  # ===== NUOVO: dati completi
    }

# ── Endpoint: Esporta sessione a JSON ────────────────────────────
 
@app.get("/session/{session_id}/export")
def export_session(session_id: str):
    """
    Esporta i dati completi della sessione a JSON.
    Utile per analytics, debug, o backup.
    """
    if session_id not in sessions:
        raise HTTPException(status_code=404, detail="Sessione non trovata.")
    
    session_mgr = sessions[session_id]
    save_session_to_json(session_id, session_mgr)
    
    return {
        "status": "exported",
        "session_id": session_id,
        "filepath": f"{SESSIONS_DIR}/{session_id}.json",
        "data": session_mgr.get_session_data()
    }


# ── Endpoint: health check ───────────────────────────────────────────────

@app.get("/health")
def health():
    return {"status": "ok", "sessions_active": len(sessions)}


# ── Avvio del server ─────────────────────────────────────────────────────

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=8000)