import numpy as np
from pgmpy.models import DiscreteBayesianNetwork
from pgmpy.factors.discrete import TabularCPD
from pgmpy.inference import VariableElimination
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple
from enum import Enum

from normalization_module import (
    ChapterNormalizer, NormalizedMetrics, ErrorBin, TimeBin
)

# ---------------------------------------------------------------------------
# Enumerazioni e costanti
# ---------------------------------------------------------------------------

class SkillLevel(Enum):
    EXPERT = 0
    INTERMEDIATE = 1
    NOVICE = 2

# serve per i capitoli opzionali -> determina l'orinde in cui mostrarli
class OptionalStatus(Enum): 
    NEVER_SHOWN = "never_shown"
    ACTIVE      = "active"
    REMOVED     = "removed"
    MASTERED    = "mastered"

# serve per impedire al sistema di togliere aiuti troppo velocemente 
    # alla prima iter il giocatore è molto guida => commette pochi errori => potrebbe essere valutato come esperto subito
class TrainingPhase(Enum):
    """Fasi di addestramento con logiche diverse."""
    FAMILIARIZATION = "familiarization"
    CONSOLIDATION = "consolidation"          
    AUTOMATION = "automation"

# serve per decidere da quale livello di difficioltà parte l'espetrienza
class InitialActivationPolicy(Enum):
    BASE = "base"
    INTERMEDIATE = "intermediate"
    ADVANCED = "advanced"

# ---------------------------------------------------------------------------
# CPT dalla letteratura
# ---------------------------------------------------------------------------

# Prior P(Skill) - forte su Novice perché tester sono principalmente principianti
PRIOR_SKILL = [0.05, 0.15, 0.80]  # [Expert, Intermediate, Novice]

# P(Errori | Skill) - righe = livelli di Errori, colonne = livelli di Skill
# Costruita con modello Poisson, λ={2, 3, 5} per {Expert, Intermediate, Novice}
# MODIFICA questi valori dopo aver riscalato i bin al tuo dominio
# CPT_ERRORS_GIVEN_SKILL = [
#     # Expert  Intermediate  Novice
#     [0.677,   0.423,        0.125],  # P(Errori=Basso | Skill)
#     [0.268,   0.433,        0.375],  # P(Errori=Medio | Skill)
#     [0.055,   0.144,        0.500],  # P(Errori=Alto  | Skill)
# ]

# # P(Tempo | Skill) - righe = livelli di Tempo, colonne = livelli di Skill
# # Costruita con modello Gaussiano, μ={5.6, 7.5, 10} min, σ={0.8, 1.05, 1.4}
# # MODIFICA questi valori dopo aver riscalato i bin al tuo dominio
# CPT_TIME_GIVEN_SKILL = [
#     # Expert  Intermediate  Novice
#     [0.750,   0.280,        0.060],  # P(Tempo=Breve  | Skill)
#     [0.220,   0.430,        0.200],  # P(Tempo=Medio  | Skill)
#     [0.030,   0.290,        0.740],  # P(Tempo=Lungo  | Skill)
# ]

# ----- Versione 3 con errori/tempo più bilanciati ------
CPT_ERRORS_GIVEN_SKILL = [
    [0.780, 0.400, 0.080],  # Low
    [0.180, 0.420, 0.280],  # Medium
    [0.040, 0.180, 0.640],  # High
]

CPT_TIME_GIVEN_SKILL = [
    [0.580, 0.300, 0.100],  # Short
    [0.300, 0.420, 0.250],  # Medium
    [0.120, 0.280, 0.650],  # Long
]


# ---------------------------------------------------------------------------
# Configurazione capitoli
# ---------------------------------------------------------------------------

@dataclass
# classe per la fase di addestramento (FAMILIARIZATION, CONSOLIDATION, AUTOMATION)
class PhaseConfig:
    """Configurazione per ogni fase."""
    phase: TrainingPhase
    iteration_count: int
    feedback_strategy: str  # "static" o "dynamic"
    allow_optional: bool
    alpha: float

# classe per la difficoltà iniziale (BASE, INTERMEDIATE, ADVANCE)
@dataclass
class InitialProfileConfig:
    name: InitialActivationPolicy
    optional_to_activate: int
    starting_phase: TrainingPhase
    description: str = ""

# classe per i dettagli di ogni capitolo
@dataclass
class ChapterConfig:
    """Configurazione di un singolo capitolo/task."""
    chapter_id: str
    name: str
    is_mandatory: bool
    # Peso del capitolo nella decisione globale (opzionali pesano meno)
    weight: float = 1.0
    # CPT personalizzate per questo capitolo (None = usa quelle di default)
    cpt_errors: Optional[List[List[float]]] = None
    cpt_time: Optional[List[List[float]]] = None
    # Numero massimo di iterazioni prima di passare oltre
    max_iterations: int = 5
    # False se non ha livelli di difficoltà
    has_difficulty_level: bool = False

    # parametri per il normalizzatore
    max_possible_errors: Optional[int] = None
    min_expected_time_sec: Optional[float] = None
    max_expected_time_sec: Optional[float] = None

# classe per lo stato attuale del capitolo
@dataclass
class ChapterState:
    """Stato corrente di un capitolo per un utente specifico."""
    chapter_id: str
    # Posterior corrente su Skill [Expert, Intermediate, Novice]
    skill_posterior: List[float] = field(
        default_factory=lambda: PRIOR_SKILL.copy()
    )
    # Storico delle osservazioni (errori, tempo) per questo capitolo
    observations: List[Tuple[int, float]] = field(default_factory=list)
    # Contatore iterazioni
    iteration_count: int = 0
    # Livello di feedback attuale (2=nessuno, 1=highlight, 0=highlight+istruzioni)
    feedback_level: int = 0
    # versione capitolo: base, avanzato
    difficulty_version: int = 0  # 0=base, 1=avanzato
    # Il capitolo è attualmente attivo nella sessione?
    is_active: bool = True
    seen_this_iter: bool = False
    optional_status: OptionalStatus = OptionalStatus.NEVER_SHOWN


# ---------------------------------------------------------------------------
# Bayesian Network per un singolo capitolo
# ---------------------------------------------------------------------------

class ChapterBN:
    """
    BN con tre nodi: Skill -> Errori, Skill -> Tempo.
    Usa VariableElimination per l'inferenza esatta.
    """

    def __init__(self, config: ChapterConfig):
        self.config = config
        self.model = self._build_model()
        self.inference_engine = VariableElimination(self.model)

    def _build_model(self) -> DiscreteBayesianNetwork:
        """Costruisce la BN con le CPT specificate."""
        model = DiscreteBayesianNetwork([
            ("Skill", "Errors"),
            ("Skill", "Time")
        ])

        # Usa CPT personalizzate se fornite, altrimenti quelle di default
        cpt_errors = self.config.cpt_errors or CPT_ERRORS_GIVEN_SKILL
        cpt_time = self.config.cpt_time or CPT_TIME_GIVEN_SKILL

        # CPD per Skill (nodo radice - prior)
        cpd_skill = TabularCPD(
            variable="Skill",
            variable_card=3,
            values=[[p] for p in PRIOR_SKILL],
            state_names={"Skill": ["Expert", "Intermediate", "Novice"]}
        )

        # CPD per Errori condizionato a Skill
        cpd_errors = TabularCPD(
            variable="Errors",
            variable_card=3,
            values=cpt_errors,
            evidence=["Skill"],
            evidence_card=[3],
            state_names={
                "Errors": ["Low", "Medium", "High"],
                "Skill": ["Expert", "Intermediate", "Novice"]
            }
        )

        # CPD per Tempo condizionato a Skill
        cpd_time = TabularCPD(
            variable="Time",
            variable_card=3,
            values=cpt_time,
            evidence=["Skill"],
            evidence_card=[3],
            state_names={
                "Time": ["Short", "Medium", "Long"],
                "Skill": ["Expert", "Intermediate", "Novice"]
            }
        )

        model.add_cpds(cpd_skill, cpd_errors, cpd_time)
        assert model.check_model(), "CPT non valide: le colonne non sommano a 1"
        return model

    def infer_skill(
        self,
        error_bin: ErrorBin,
        time_bin: TimeBin,
        prior_override: Optional[List[float]] = None
    ) -> List[float]:
        """
        Inferisce la distribuzione posteriore su Skill dati errori e tempo.

        Se prior_override è fornito, sostituisce la prior di default nella BN
        prima dell'inferenza - questo è il meccanismo di aggiornamento
        sequenziale tra iterazioni.

        Restituisce [P(Expert), P(Intermediate), P(Novice)].
        """
        # Se c'è una prior da iterazione precedente, aggiorna temporaneamente
        # la CPD di Skill prima dell'inferenza
        if prior_override is not None:
            self._update_skill_prior(prior_override)

        # Mappa i bin agli stati della BN
        error_state = ["Low", "Medium", "High"][error_bin.value]
        time_state = ["Short", "Medium", "Long"][time_bin.value]

        # Inferenza con evidenza su entrambi i nodi osservabili
        result = self.inference_engine.query(
            variables=["Skill"],
            evidence={"Errors": error_state, "Time": time_state},
            show_progress=False
        )

        # Estrai le probabilità nell'ordine [Expert, Intermediate, Novice]
        posterior = [
            float(result.get_value(Skill="Expert")),
            float(result.get_value(Skill="Intermediate")),
            float(result.get_value(Skill="Novice"))
        ]
        return posterior

    def _update_skill_prior(self, new_prior: List[float]):
        """
        Aggiorna la CPD di Skill con una nuova prior.
        Questo implementa l'aggiornamento sequenziale tra iterazioni.
        """
        new_cpd_skill = TabularCPD(
            variable="Skill",
            variable_card=3,
            values=[[p] for p in new_prior],
            state_names={"Skill": ["Expert", "Intermediate", "Novice"]}
        )
        self.model.remove_cpds(self.model.get_cpds("Skill"))
        self.model.add_cpds(new_cpd_skill)
        # Ricrea il motore di inferenza con la CPD aggiornata
        self.inference_engine = VariableElimination(self.model)


# ---------------------------------------------------------------------------
# Skill label
# ---------------------------------------------------------------------------

def most_likely_skill(posterior: List[float]) -> SkillLevel:
    """Restituisce il livello di skill con probabilità massima (MAP)."""
    return SkillLevel(int(np.argmax(posterior)))

def skill_label(posterior: List[float]) -> str:
    """Etichetta leggibile del livello di skill più probabile."""
    labels = ["Expert", "Intermediate", "Novice"]
    idx = int(np.argmax(posterior))
    return f"{labels[idx]} ({posterior[idx]:.2f})"


# ---------------------------------------------------------------------------
# Regole di decisione adattiva
# ---------------------------------------------------------------------------

class AdaptiveDecisionEngine:
    """
    Traduce le posterior della BN in decisioni concrete del sistema adattivo.
    Tutte le soglie sono modificabili - aggiustale in base ai test con gli utenti.
    """
    MASTERY_THRESHOLD        = 0.80 # se il valore di Expert > il capitolo è considerato padroneggiato
    STRUGGLE_THRESHOLD       = 0.70 # se Novice > allora FEEDBACK massimo
    ADD_OPTIONAL_THRESHOLD   = 0.60 # se expert + intermediate > allora aggiungo un opzionale
    REMOVE_OPTIONAL_THRESHOLD= 0.60 # se il valore di Novice > il capitolo è considerato difficile
    MIN_CONSECUTIVE_FOR_CHANGE = 1 # dopo quante osservazioni del capitolo compio decisioni di aggiunta/rimozione

    @staticmethod
    def determine_feedback_level(
        posterior: List[float],
        current_level: int,
        had_recent_struggle: bool,
        phase_config: PhaseConfig,
        mastered: bool = False
    ) -> int:
        p_novice = posterior[SkillLevel.NOVICE.value]
        p_intermediate = posterior[SkillLevel.INTERMEDIATE.value]
        p_expert = posterior[SkillLevel.EXPERT.value]

        # ===== FASE 1: Feedback sempre massimo =====
        # Il feedback può essere ridotto da massimo a intermedio, ma non eliminato
        if phase_config.feedback_strategy == "static":
            print("=== FAMILIARIZAZION PHASE: static feedback ===")
            if p_novice > AdaptiveDecisionEngine.STRUGGLE_THRESHOLD:
                return 0
            if  (p_intermediate > p_novice):
                return 1
            return current_level
        
        if mastered:
            return 0
        
        # ===== FASE 2 e 3: Feedback variabile =====
        # nelle altre due fasi può cambiare liberamente
        if p_novice > AdaptiveDecisionEngine.STRUGGLE_THRESHOLD:
          return 0
        if  (p_intermediate > p_novice and p_intermediate > p_expert):
          return 1
        if p_expert > p_intermediate:
          if not had_recent_struggle:
              return 2
          return 2
        return current_level

    @staticmethod
    def chapter_is_good(posterior: List[float]) -> bool:
        """
        True se il capitolo supera la soglia per considerarsi
        'andato bene' ai fini della decisione globale.
        """
        p_capable = (posterior[SkillLevel.EXPERT.value]
                     + posterior[SkillLevel.INTERMEDIATE.value])
        return p_capable > AdaptiveDecisionEngine.ADD_OPTIONAL_THRESHOLD

    @staticmethod
    def chapter_is_struggling(posterior: List[float]) -> bool:
        """
        True se il capitolo mostra difficoltà significative.
        """
        return (posterior[SkillLevel.NOVICE.value]
                > AdaptiveDecisionEngine.REMOVE_OPTIONAL_THRESHOLD)

# ---------------------------------------------------------------------------
# Manager principale del sistema adattivo
# ---------------------------------------------------------------------------

@dataclass
class AdaptiveDecision:
    """Risultato di una decisione adattiva dopo una osservazione."""
    chapter_id: str
    skill_posterior: List[float]
    skill_label: str
    new_feedback_level: int
    new_difficulty_level: int
    feedback_changed: bool
    difficulty_changed: bool
    add_optional: bool
    added_chapter_id: Optional[str]   # None se nessun capitolo aggiunto
    removed_chapter_id: Optional[str]   # None se nessun capitolo rimosso
    remove_optional: bool
    chapter_mastered: bool
    message: str


class AdaptiveTrainingManager:
    # parametri per early remove dei capitoli opzionali:
    EARLY_REMOVE_MIN_FRACTION_COMPLETED = 0.2 # almeno 40% completati (ho abbastanza dati da analizzare)
    EARLY_REMOVE_STRUGGLE_FRACTION      = 0.5 # più del 50% in difficoltà

    # Configurazioni delle fasi
    PHASE_CONFIGS = {
        TrainingPhase.FAMILIARIZATION: PhaseConfig(
            phase=TrainingPhase.FAMILIARIZATION,
            iteration_count = 1,
            feedback_strategy="limited",
            allow_optional=False,
            alpha = 0.3 # le osservazioni influnzano al 30% le nuove prior
        ),
        TrainingPhase.CONSOLIDATION: PhaseConfig(
            phase=TrainingPhase.CONSOLIDATION,
            iteration_count = 1,
            feedback_strategy="dynamic",
            allow_optional=True,
            alpha = 0.3 # le osservazioni influnzano al 50% le nuove prior
        ),
        TrainingPhase.AUTOMATION: PhaseConfig(
            phase=TrainingPhase.AUTOMATION,
            iteration_count = 999,
            feedback_strategy="dynamic",
            allow_optional=True,
            alpha = 1.0
        ),
    }

    # Initialization policy
    INITIAL_PROFILES = {
    InitialActivationPolicy.BASE: InitialProfileConfig(
        name=InitialActivationPolicy.BASE,
        optional_to_activate=0,
        starting_phase=TrainingPhase.FAMILIARIZATION,
        description="Solo obbligatori, fase guidata"
    ),
    InitialActivationPolicy.INTERMEDIATE: InitialProfileConfig(
        name=InitialActivationPolicy.INTERMEDIATE,
        optional_to_activate=1,
        starting_phase=TrainingPhase.AUTOMATION,
        description="Alcuni opzionali attivi, ma fase ancora guidata"
    ),
    InitialActivationPolicy.ADVANCED: InitialProfileConfig(
        name=InitialActivationPolicy.ADVANCED,
        optional_to_activate=3,
        starting_phase=TrainingPhase.CONSOLIDATION,
        description="Più opzionali e fase meno guidata"
    ),
}


    def __init__(self, chapter_configs: List[ChapterConfig], initial_policy: InitialActivationPolicy):

        self.profile_config = self.INITIAL_PROFILES[initial_policy] # da quale difficoltà inizio
        self.current_phase = self.profile_config.starting_phase # in quale fase di training mi trovo
        self.phase_iteration_count = 0 # numero iterazioni dall'avvio della fase corrente

        # dizionario per tutti i capitoli
        self.configs = {c.chapter_id: c for c in chapter_configs}
        self.mandatory_ids = [
            c.chapter_id for c in chapter_configs if c.is_mandatory
        ]
        self.optional_ids = [
            c.chapter_id for c in chapter_configs if not c.is_mandatory
        ]

        # Crea una BN per ogni capitolo
        self.chapter_bns: Dict[str, ChapterBN] = {
            cid: ChapterBN(config)
            for cid, config in self.configs.items()
        }

        # Stato corrente per ogni capitolo
        self.chapter_states: Dict[str, ChapterState] = {
            cid: ChapterState(chapter_id=cid)
            for cid in self.configs
        }

        # Normalizzatore
        self.normalizer = ChapterNormalizer()
        
        # Registra la complessità di ogni capitolo
        for cfg in chapter_configs:
            if (cfg.max_possible_errors is not None
                and cfg.min_expected_time_sec is not None
                and cfg.max_expected_time_sec is not None):
                self.normalizer.register_chapter(
                    chapter_id=cfg.chapter_id,
                    max_possible_errors=cfg.max_possible_errors,
                    min_expected_time_sec=cfg.min_expected_time_sec,
                    max_expected_time_sec=cfg.max_expected_time_sec,
                )

        print("=== INIT SESSION ===")

        self._apply_initial_activation()

        for cid, state in self.configs.items():
            print(cid, "mandatory:", state.is_mandatory,
                "active:", self.chapter_states[cid].is_active)

        print("OPTIONAL IDS:", self.optional_ids)

        # ── Nuovi contatori globali ──────────────────────────────────────
        self._consecutive_good_global:     int = 0
        self._consecutive_struggle_global: int = 0

        # Accumula i risultati dei capitoli nell'iterazione corrente.
        # Chiave = chapter_id, Valore = True (buono) / False (difficoltà)
        self._current_iteration_results: Dict[str, bool] = {}

        # Traccia se nell'iterazione corrente c'è stata almeno una
        # difficoltà su un singolo capitolo (usato per il feedback locale)
        self._had_struggle_this_iter: Dict[str, bool] = {
            cid: False for cid in self.configs
        }

        # flag rimozione anticipata per l'iterazione corrente
        self._early_remove_done_this_iter: bool = False
    
    def _apply_initial_activation(self):
        
        # 1️⃣ Attiva sempre obbligatori
        for cid, state in self.configs.items():
            if state.is_mandatory:
                self.chapter_states[cid].is_active = True
            else:
                self.chapter_states[cid].is_active = False

        # # 2️⃣ Attiva N opzionali
        # optional_chapters = [
        #     c for c in self.chapter_states.values()
        #     if not c.is_mandatory
        # ]

        for cid in self.optional_ids[:self.profile_config.optional_to_activate]:
            self.chapter_states[cid].is_active = True
            self._activate_next_optional(phase_config= self.PHASE_CONFIGS[self.profile_config.starting_phase])

        # 3️⃣ Imposta fase iniziale
        self.current_phase = self.profile_config.starting_phase
        print(f"=== INITIAL ACTIVATION POLICY: {self.profile_config.name} ===")
        print(f"=== TRAINING PHASE: {self.current_phase} ===")
    
    def _check_phase_transition(self, phase_config: PhaseConfig):

        if self.phase_iteration_count >= phase_config.iteration_count:
            self._advance_phase()
    
    def _advance_phase(self):

        old_phase = self.current_phase

        if self.current_phase == TrainingPhase.FAMILIARIZATION:
            self.current_phase = TrainingPhase.CONSOLIDATION
        elif self.current_phase == TrainingPhase.CONSOLIDATION:
            self.current_phase = TrainingPhase.AUTOMATION

        self.phase_iteration_count = 0

        print(f"[PHASE] Transition: {old_phase} → {self.current_phase}")
    
    # def get_current_phase(self, iteration_number: int) -> TrainingPhase:
    #     """
    #     Determina la fase basata sul numero di iterazione.
        
    #     una iter: FAMILIARIZATION
    #     una iter: CONSOLIDATION
    #     tutte le altre iter: AUTOMATION
    #     """
    #     if iteration_number <= 2:
    #         return TrainingPhase.FAMILIARIZATION
    #     elif iteration_number <= 4:
    #         return TrainingPhase.CONSOLIDATION
    #     else:
    #         return TrainingPhase.AUTOMATION


    def observe(
        self,
        chapter_id: str,
        chapter_name: str,
        errors: int,
        time_sec: float,
        iteration_number: int
    ) -> AdaptiveDecision:

        state = self.chapter_states[chapter_id]
        bn    = self.chapter_bns[chapter_id]

        # ── 1. Discretizza ───────────────────────────────────────────────

        # ==== VECCHIO ====
        #error_bin = discretize_errors(errors)
        #time_bin  = discretize_time(time_sec)

        # ===== NUOVO: Normalizzazione =====
        metrics = self.normalizer.normalize(chapter_id, errors, time_sec)
        error_bin = metrics.error_bin
        time_bin = metrics.time_bin
        
        # ── 2. Aggiornamento Bayesiano sequenziale ───────────────────────
        # Alla prima iterazione usa la prior di default (None).
        # Dalle iterazioni successive usa la posterior precedente come
        # nuova prior, implementando l'aggiornamento sequenziale.

        # Smoothing della prior verso la distribuzione originale
        beta = self.PHASE_CONFIGS[self.current_phase].alpha  # riusi alpha come parametro
        original_prior = PRIOR_SKILL
        prev = state.skill_posterior

        # La prior iniettata è una versione attenuata della posterior precedente
        smoothed_prior = [beta * prev[i] + (1 - beta) * original_prior[i] for i in range(3)]

        # La BN usa la prior attenuata, nessun blending dopo
        new_posterior = bn.infer_skill(error_bin, time_bin, prior_override=smoothed_prior)
        state.skill_posterior = new_posterior
        state.observations.append((errors, time_sec))
        state.iteration_count += 1
        state.seen_this_iter = True

        # ── 3. Feedback locale ───────────────────────────────────────────
        # Il feedback dipende solo dalla posterior di questo capitolo.
        # had_recent_struggle è True se il capitolo ha mostrato difficoltà

        # Aggiorna il flag di difficoltà per questo capitolo
        # p(Novice_cap) > AdaptiveDecisionEngine.REMOVE_OPTIONAL_THRESHOLD
        if AdaptiveDecisionEngine.chapter_is_struggling(new_posterior):
            self._had_struggle_this_iter[chapter_id] = True

        had_struggle = self._had_struggle_this_iter.get(chapter_id, False)
        old_feedback = state.feedback_level
        new_feedback = AdaptiveDecisionEngine.determine_feedback_level(
            new_posterior, old_feedback, had_struggle, phase_config= self.PHASE_CONFIGS[self.current_phase]
        )
        state.feedback_level = new_feedback


        # ── 4. Registra risultato nell'iterazione corrente ───────────────
        # chapter_is_good() restituisce True se P(Expert)+P(Intermediate)
        # supera ADD_OPTIONAL_THRESHOLD per questo capitolo.
        # Usiamo il risultato peggiore tra tutte le osservazioni dello stesso
        # capitolo nella stessa iterazione (per robustezza).
        #
        # p(expert_cap) + p(intermediate_cap) > AdaptiveDecisionEngine.ADD_OPTIONAL_THRESHOLD
        current_result = AdaptiveDecisionEngine.chapter_is_good(new_posterior)
        if chapter_id in self._current_iteration_results:
            # Se il capitolo è già stato osservato in questa iterazione
            # (non dovrebbe succedere con l'uso normale, ma gestiamo il caso),
            # prendiamo il risultato più conservativo (AND logico).
            self._current_iteration_results[chapter_id] = (
                self._current_iteration_results[chapter_id] and current_result
            )
        else:
            self._current_iteration_results[chapter_id] = current_result # sara True se p(expert_cap) + p(intermediate_cap) > AdaptiveDecisionEngine.ADD_OPTIONAL_THRESHOLD

        # ── 5. Valutazione globale se l'iterazione è completa ────────────
        # L'iterazione è completa quando tutti i capitoli
        # attivi hanno un risultato registrato in _current_iteration_results.
        active_chapters = [
            cid for cid in self.configs
            if self.chapter_states[cid].is_active
        ]
        iteration_complete = all(
            cid in self._current_iteration_results
            for cid in active_chapters
        )

        add_optional    = False
        remove_optional = False
        removed_chapter_id = None
        added_chapter_id = None

        if iteration_complete:
            # Calcola la frazione di capitoli attivi andati bene
            good_count = sum(
                1 for cid in active_chapters
                if self._current_iteration_results.get(cid, False)
            )
            fraction_good = good_count / len(active_chapters)

            # Quanti mostrano difficoltà
            struggle_count = sum(
                1 for cid in active_chapters
                if AdaptiveDecisionEngine.chapter_is_struggling(
                    self.chapter_states[cid].skill_posterior
                )
            )
            fraction_struggle = struggle_count / len(active_chapters)

            # Aggiorna i contatori globali
            min_c = AdaptiveDecisionEngine.MIN_CONSECUTIVE_FOR_CHANGE

            if fraction_good >= 0.70:
                # Iterazione positiva: incrementa il contatore buono,
                # azzera quello di difficoltà
                self._consecutive_good_global     += 1
                self._consecutive_struggle_global  = 0

            elif fraction_struggle >= 0.50:
                # Iterazione difficile: incrementa il contatore difficoltà,
                # azzera quello buono
                self._consecutive_struggle_global += 1
                self._consecutive_good_global      = 0

            else:
                # Zona intermedia: decrementa entrambi verso zero.
                # Non azzera di colpo per evitare oscillazioni.
                self._consecutive_good_global     = max(
                    0, self._consecutive_good_global - 1
                )
                self._consecutive_struggle_global = max(
                    0, self._consecutive_struggle_global - 1
                )

            # Decisione: aggiungere un opzionale?
            # Condizione: abbastanza iterazioni consecutive positive
            if self._consecutive_good_global >= min_c:
                activated = self._activate_next_optional(phase_config= self.PHASE_CONFIGS[self.current_phase])
                if activated:
                    add_optional = True
                    added_chapter_id = activated
                    # Reset del contatore dopo l'azione per evitare
                    # di aggiungere più opzionali alla stessa iterazione
                    self._consecutive_good_global = 0

            # Decisione: rimuovere un opzionale?
            # Condizione: abbastanza iterazioni consecutive difficili
            if self._consecutive_struggle_global >= min_c:
                if not self._early_remove_done_this_iter:
                    removed = self._deactivate_weakest_optional(phase_config= self.PHASE_CONFIGS[self.current_phase])
                    if removed:
                        remove_optional = True
                        removed_chapter_id = removed
                        self._consecutive_struggle_global = 0

            # Reset per la prossima iterazione
            self._current_iteration_results = {}
            # Reset del flag di difficoltà locale per la prossima iterazione
            self._had_struggle_this_iter = {cid: False for cid in self.configs}
            self._early_remove_done_this_iter = False
            self.phase_iteration_count += 1
            self.reset_seen_chapters()
            self._check_phase_transition(self.PHASE_CONFIGS[self.current_phase])

        early_remove = False  # sempre inizializzata

        if not iteration_complete:
          if not self._early_remove_done_this_iter:
            early_remove = self._should_remove_optional_early(phase_config = self.PHASE_CONFIGS[self.current_phase])

        # Rimozione anticipata
        if early_remove:
            removed = self._deactivate_weakest_optional(phase_config = self.PHASE_CONFIGS[self.current_phase])
            if removed:
                remove_optional = True
                removed_chapter_id = removed
                self._early_remove_done_this_iter = True

        # ── 6. Verifica padronanza del singolo capitolo ──────────────────
        # Un capitolo è padroneggiato se P(Expert) supera MASTERY_THRESHOLD.
        # Non usiamo isteresi qui perché la padronanza è una proprietà
        # del singolo capitolo, non dell'iterazione completa.

        
        old_difficulty = state.difficulty_version
        new_difficulty = state.difficulty_version  # Default: no change

        chapter_mastered = (
            new_posterior[SkillLevel.EXPERT.value]
            > AdaptiveDecisionEngine.MASTERY_THRESHOLD
        )
        if chapter_mastered and chapter_id and self.configs[chapter_id].has_difficulty_level and self.chapter_states[chapter_id].difficulty_version == 0:
            self.chapter_states[chapter_id].optional_status = OptionalStatus.MASTERED
            self.chapter_states[chapter_id].difficulty_version = 1
            new_difficulty = 1
            # cambia di nuovo il feedback
            new_feedback = AdaptiveDecisionEngine.determine_feedback_level(
                new_posterior, old_feedback, had_struggle, phase_config= self.PHASE_CONFIGS[self.current_phase], mastered=True)
            
            state.feedback_level = new_feedback
        # ── Costruisci il messaggio di log ───────────────────────────────
        message = self._build_message(
            chapter_id, chapter_name, new_posterior, new_feedback, old_feedback, old_difficulty, new_difficulty,
            add_optional, remove_optional, early_remove, chapter_mastered, iteration_complete
        )

        return AdaptiveDecision(
            chapter_id=chapter_id,
            skill_posterior=new_posterior,
            skill_label=skill_label(new_posterior),
            new_feedback_level=new_feedback,
            new_difficulty_level=new_difficulty,
            feedback_changed=(new_feedback != old_feedback),
            difficulty_changed=(new_difficulty!=old_difficulty),
            add_optional=add_optional,
            added_chapter_id = added_chapter_id,
            removed_chapter_id = removed_chapter_id,
            remove_optional=remove_optional,
            chapter_mastered=chapter_mastered,
            message=message
        )
    
    def reset_seen_chapters(self):
        for cid, state in self.chapter_states.items():
            state.seen_this_iter = False

    def get_active_chapters(self) -> List[str]:
        """Restituisce gli ID dei capitoli attualmente attivi."""
        return [
            cid for cid, s in self.chapter_states.items()
            if s.is_active
        ]

    def get_chapter_summary(self) -> Dict[str, dict]:
        """Restituisce un riepilogo dello stato di tutti i capitoli."""
        summary = {}
        for cid, state in self.chapter_states.items():
            summary[cid] = {
                "name": self.configs[cid].name,
                "is_mandatory": self.configs[cid].is_mandatory,
                "is_active": state.is_active,
                "iterations": state.iteration_count,
                "current_skill": skill_label(state.skill_posterior),
                "posterior": {
                    "Expert": round(state.skill_posterior[0], 3),
                    "Intermediate": round(state.skill_posterior[1], 3),
                    "Novice": round(state.skill_posterior[2], 3),
                },
                "feedback_level": state.feedback_level,
            }
        return summary

    def _activate_next_optional(self, phase_config: PhaseConfig) -> Optional[str]:
        """
        Seleziona il prossimo opzionale da attivare secondo la logica:
        1. Prima i never_shown nell'ordine originale
        2. Poi i removed ordinati per P(Expert) crescente
        """
        # ===== Deve essere in fase AUTOMATION =====
        if not phase_config.allow_optional:
            print("=== FAMILIARIZAZION PHASE: no add optional")
            return None
        
        # Lista A: never_shown nell'ordine originale
        never_shown = [
            cid for cid in self.optional_ids
            if self.chapter_states[cid].optional_status == OptionalStatus.NEVER_SHOWN
        ]
        if never_shown:
            chosen = never_shown[0]  # ordine originale preservato
            f"BRO: never_shown pieno: {chosen}"
            self.chapter_states[chosen].is_active = True
            self.chapter_states[chosen].optional_status = OptionalStatus.ACTIVE
            return chosen
        else:
            print("BRO: never_shown vuoto")

        # Lista B: removed ordinati per P(Expert) crescente
        # (mastery minore prima, così l'utente lavora sulle debolezze)
        removed = [
            cid for cid in self.optional_ids
            if self.chapter_states[cid].optional_status == OptionalStatus.REMOVED
        ]
        if not removed:
            return None  # nessun capitolo disponibile: terminazione ideale

        removed_sorted = sorted(
            removed,
            key=lambda cid: self.chapter_states[cid].skill_posterior[
                SkillLevel.EXPERT.value
            ]
        )
        chosen = removed_sorted[0]
        self.chapter_states[chosen].is_active = True
        self.chapter_states[chosen].optional_status = OptionalStatus.ACTIVE
        return chosen

    def _deactivate_weakest_optional(self, phase_config: PhaseConfig) -> Optional[str]:
        """
        Rimuove l'opzionale con P(Expert) massima tra quelli attivi
        e non ancora padroneggiati.
        """
        if not phase_config.allow_optional:
            print("=== FAMILIARIZAZION PHASE: no remove optional")
            return None
    
        active_optionals = [
            cid for cid in self.optional_ids
            if (self.chapter_states[cid].is_active
                and self.chapter_states[cid].optional_status != OptionalStatus.MASTERED)
        ]
        if not active_optionals:
            return None
        
        not_completed_optionals = [
            cid for cid in self.optional_ids
            if (self.chapter_states[cid].is_active
                and self.chapter_states[cid].optional_status != OptionalStatus.MASTERED
                and not self.chapter_states[cid].seen_this_iter)
        ]

        if not_completed_optionals:
            eligible_optionals = not_completed_optionals
        else:
            eligible_optionals = active_optionals

        best = max(
            eligible_optionals,
            key=lambda cid: self.chapter_states[cid].skill_posterior[
                SkillLevel.EXPERT.value
            ]
        )
        self.chapter_states[best].is_active = False
        self.chapter_states[best].optional_status = OptionalStatus.REMOVED
        return best

    def _should_remove_optional_early(self, phase_config: PhaseConfig) -> bool:
        # ===== Deve essere in fase AUTOMATION =====
        if not phase_config.allow_optional:
            print("=== FAMILIARIZAZION PHASE: no early remove")
            return False
        
        # procedi solo se ho eseguito almeno la metà dei capitoli totali attivi
        active_chapters = [cid for cid in self.configs if self.chapter_states[cid].is_active]
        completed_chapters = [cid for cid in active_chapters if cid in self._current_iteration_results]

        if len(completed_chapters) / len(active_chapters) \
                < self.EARLY_REMOVE_MIN_FRACTION_COMPLETED:
                return False # non ho abbsatnza dati

        # Per ciascuno: controlla se sono struggling
        struggling = [
            cid for cid in completed_chapters
            if AdaptiveDecisionEngine.chapter_is_struggling(
                self.chapter_states[cid].skill_posterior
            )
        ]
        return (len(struggling) / len(completed_chapters)
                    > self.EARLY_REMOVE_STRUGGLE_FRACTION)

    def _build_message(
        self, chapter_id, chapter_name, posterior, new_fb, old_fb, old_dl, new_dl,
        add_opt, rem_opt, early_rem, mastered, iter_complete
    ) -> str:
        labels = ["Expert", "Intermediate", "Novice"]
        skill  = labels[int(np.argmax(posterior))]
        fb_desc = ["nessun aiuto", "solo highlight", "highlight + istruzioni"]
        dl_desc = ["versione base", "versione avanzata"]
        parts = [
            f"Capitolo {chapter_name}: skill stimata = {skill} "
            f"({posterior[int(np.argmax(posterior))]:.0%})"
            #f"(All posterior: {posterior})"
        ]
        if new_fb != old_fb:
            parts.append(f"Feedback: {fb_desc[old_fb]} → {fb_desc[new_fb]}")
        if new_dl != old_dl:
            parts.append(f"Difficulty level: {fb_desc[old_fb]} → {fb_desc[new_fb]}")
        if mastered:
            parts.append("✓ Capitolo padroneggiato")
        if iter_complete:
            parts.append(
                f"[iter completa | "
                f"buoni_glob={self._consecutive_good_global} "
                f"diff_glob={self._consecutive_struggle_global}]"
            )
        if add_opt:
            parts.append("+ Aggiunto capitolo opzionale")
        if rem_opt:
            if early_rem:
              parts.append("- Rimosso capitolo opzionale per early_remove")
            else:
              parts.append("- Rimosso capitolo opzionale")
        return " | ".join(parts)