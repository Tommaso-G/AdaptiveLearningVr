import numpy as np
from pgmpy.models import DiscreteBayesianNetwork
from pgmpy.factors.discrete import TabularCPD
from pgmpy.inference import VariableElimination
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple
from enum import Enum
from pathlib import Path
import json

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
PRIOR_SKILL = [1/3, 1/3, 1/3]  # [Expert, Intermediate, Novice]

# Dalla letteratura
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

# Rapporto discriminante ~8x per entrambi — posizione neutrale (Versione 2)
# CPT_ERRORS_GIVEN_SKILL = [
#     # Expert  Intermediate  Novice
#     [0.750,   0.400,        0.090],  # Low   — ratio: 0.750/0.090 = 8.3x
#     [0.210,   0.420,        0.310],  # Medium
#     [0.040,   0.180,        0.600],  # High  — ratio: 0.600/0.040 = 15x (novice end)
# ]

# CPT_TIME_GIVEN_SKILL = [
#     # Expert  Intermediate  Novice
#     [0.630,   0.290,        0.080],  # Short — ratio: 0.630/0.080 = 7.9x
#     [0.280,   0.420,        0.240],  # Medium
#     [0.090,   0.290,        0.680],  # Long  — ratio: 0.680/0.090 = 7.6x
# ]

# Versione 3 dai dati
CPT_ERRORS_GIVEN_SKILL = [
    [0.765, 0.708, 0.561],  # LOW
    [0.117, 0.164, 0.231],  # MEDIUM
    [0.117, 0.128, 0.209],  # HIGH
]

CPT_TIME_GIVEN_SKILL = [
    [0.636, 0.623, 0.407],  # SHORT
    [0.154, 0.200, 0.286],  # MEDIUM
    [0.210, 0.176, 0.308],  # LONG
]

# ---------------------------------------------------------------------------
# Configurazione capitoli
# ---------------------------------------------------------------------------

@dataclass
# classe per la fase di addestramento (FAMILIARIZATION, AUTOMATION)
class PhaseConfig:
    """Configurazione per ogni fase."""
    phase: TrainingPhase
    iteration_count: int
    feedback_strategy: str  # "static" o "dynamic"
    allow_optional: bool
    allow_progression_upgrade: bool

# classe per la difficoltà iniziale (BASE, INTERMEDIATE, ADVANCE)
@dataclass
class InitialProfileConfig:
    name: InitialActivationPolicy
    optional_to_activate: int
    optional_per_progression: int # 0 -> aggiunto solo l'opzionale di defaul
    starting_phase: TrainingPhase
    description: str = ""

# classe per i dettagli di ogni capitolo
@dataclass
class ChapterConfig:
    chapter_id: str
    name: str
    is_mandatory: bool
    weight: float = 1.0
    cpt_errors: Optional[List[List[float]]] = None
    cpt_time: Optional[List[List[float]]] = None
    max_iterations: int = 5
    has_difficulty_level: bool = False

    # soglia master
    master_threshold: float = 0.6

    # parametri normalizzatore versione base
    max_possible_errors: Optional[int] = None
    min_expected_time_sec: Optional[float] = None
    max_expected_time_sec: Optional[float] = None

    # parametri versione avanzata (None = usa gli stessi della base)
    cpt_errors_advanced: Optional[List[List[float]]] = None
    cpt_time_advanced: Optional[List[List[float]]] = None
    max_possible_errors_advanced: Optional[int] = None
    min_expected_time_sec_advanced: Optional[float] = None
    max_expected_time_sec_advanced: Optional[float] = None

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

    def __init__(self, config: ChapterConfig):
        self.config = config
        self._current_difficulty = 0  # 0=base, 1=avanzata
        self.model = self._build_model(difficulty=0)
        self.inference_engine = VariableElimination(self.model)

    def _build_model(self, difficulty: int) -> DiscreteBayesianNetwork:
        model = DiscreteBayesianNetwork([
            ("Skill", "Errors"),
            ("Skill", "Time")
        ])

        if difficulty == 1 and self.config.has_difficulty_level:
            cpt_errors = self.config.cpt_errors_advanced or self.config.cpt_errors or CPT_ERRORS_GIVEN_SKILL
            cpt_time   = self.config.cpt_time_advanced   or self.config.cpt_time   or CPT_TIME_GIVEN_SKILL
        else:
            cpt_errors = self.config.cpt_errors or CPT_ERRORS_GIVEN_SKILL
            cpt_time   = self.config.cpt_time   or CPT_TIME_GIVEN_SKILL

        cpd_skill = TabularCPD(
            variable="Skill", variable_card=3,
            values=[[p] for p in PRIOR_SKILL],
            state_names={"Skill": ["Expert", "Intermediate", "Novice"]}
        )
        cpd_errors = TabularCPD(
            variable="Errors", variable_card=3,
            values=cpt_errors, evidence=["Skill"], evidence_card=[3],
            state_names={"Errors": ["Low", "Medium", "High"],
                         "Skill": ["Expert", "Intermediate", "Novice"]}
        )
        cpd_time = TabularCPD(
            variable="Time", variable_card=3,
            values=cpt_time, evidence=["Skill"], evidence_card=[3],
            state_names={"Time": ["Short", "Medium", "Long"],
                         "Skill": ["Expert", "Intermediate", "Novice"]}
        )
        model.add_cpds(cpd_skill, cpd_errors, cpd_time)
        assert model.check_model()
        return model

    def switch_to_advanced(self):
        """Ricostruisce la BN con le CPT della versione avanzata."""
        if self._current_difficulty == 1:
            return  # già avanzata, niente da fare
        self._current_difficulty = 1
        self.model = self._build_model(difficulty=1)
        self.inference_engine = VariableElimination(self.model)
        print(f"[BN] {self.config.chapter_id} → CPT versione avanzata caricate")

    # infer_skill e _update_skill_prior restano invariati

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

    SKILL_THRESHOLD_HIGH = 0.50  # buono → add optional
    SKILL_THRESHOLD_LOW  = 0.35  # difficoltà seria → remove optional
    MIN_CONSECUTIVE_FOR_CHANGE = 1 # dopo quante osservazioni del capitolo compio decisioni di aggiunta/rimozione

    @staticmethod
    def determine_feedback_level(
        posterior: List[float],
        current_level: int,
        had_recent_struggle: bool,
        phase_config: PhaseConfig,
        mastered: bool = False
    ) -> int:

        skill_score = AdaptiveDecisionEngine.compute_skill_score(posterior)

        # ===== FASE 1: static feedback =====
        if phase_config.feedback_strategy == "static":

            # se è in forte difficoltà → aiuto massimo
            if skill_score < AdaptiveDecisionEngine.SKILL_THRESHOLD_LOW:
                return 0

            # livello intermedio
            if skill_score < AdaptiveDecisionEngine.SKILL_THRESHOLD_HIGH:
                return 1
            
            # alta competenza
            if skill_score >= AdaptiveDecisionEngine.SKILL_THRESHOLD_HIGH:
                return 1
            
            return min(current_level,1)
        
        # se il capitolo è masterato → ripristina temporanemente i feedback per la versione advance
        if mastered:
            return 0

        # ===== FASE 2 e 3: dynamic feedback =====

        # forte difficoltà
        if skill_score < AdaptiveDecisionEngine.SKILL_THRESHOLD_LOW  :
            return 0

        # zona media → aiuto intermedio
        if skill_score < AdaptiveDecisionEngine.SKILL_THRESHOLD_HIGH:
            return 1

        # alta competenza
        if skill_score >= AdaptiveDecisionEngine.SKILL_THRESHOLD_HIGH:

            if had_recent_struggle:
              return max(current_level, 1)

            return 2

        return current_level

    @staticmethod
    def is_good(skill_score: float) -> bool:
        return skill_score >= AdaptiveDecisionEngine.SKILL_THRESHOLD_HIGH

    @staticmethod
    def is_struggling(skill_score: float) -> bool:
        return skill_score < AdaptiveDecisionEngine.SKILL_THRESHOLD_LOW

    @staticmethod
    def compute_skill_score(posterior: List[float]) -> float:
        p_expert = posterior[SkillLevel.EXPERT.value]
        p_intermediate = posterior[SkillLevel.INTERMEDIATE.value]

        # mapping semplice (puoi cambiarlo dopo)
        return p_expert + 0.5 * p_intermediate

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
    added_chapter_ids: List[str]    # lista vuota se nessun capitolo aggiunto
    removed_chapter_id: Optional[str]   # None se nessun capitolo rimosso
    remove_optional: bool
    chapter_mastered: bool
    message: str


class AdaptiveTrainingManager:
    # parametri per early remove dei capitoli opzionali:
    #EARLY_REMOVE_MIN_FRACTION_COMPLETED = 0.4  # almeno 40% completati (ho abbastanza dati da analizzare)
    #EARLY_REMOVE_STRUGGLE_FRACTION      = 0.5 # più del 50% in difficoltà

    # Configurazioni delle fasi
    PHASE_CONFIGS = {
        TrainingPhase.FAMILIARIZATION: PhaseConfig(
            phase=TrainingPhase.FAMILIARIZATION,
            iteration_count = 1,
            feedback_strategy="static",
            allow_optional=True,
            allow_progression_upgrade=False,
        ),
        TrainingPhase.AUTOMATION: PhaseConfig(
            phase=TrainingPhase.AUTOMATION,
            iteration_count = 999,
            feedback_strategy="dynamic",
            allow_optional=True,
            allow_progression_upgrade=True,
        ),
    }

    # Initialization policy
    INITIAL_PROFILES = {
    InitialActivationPolicy.BASE: InitialProfileConfig(
        name=InitialActivationPolicy.BASE,
        optional_to_activate=0,
        starting_phase=TrainingPhase.FAMILIARIZATION,
        optional_per_progression=0,
        description="progressione -> 1 opzionale per livello"
    ),
    InitialActivationPolicy.INTERMEDIATE: InitialProfileConfig(
        name=InitialActivationPolicy.INTERMEDIATE,
        optional_to_activate=0,
        starting_phase=TrainingPhase.FAMILIARIZATION,
        optional_per_progression=1,
        description="progressione -> 2 opzionali per livello"
    ),
    InitialActivationPolicy.ADVANCED: InitialProfileConfig(
        name=InitialActivationPolicy.ADVANCED,
        optional_to_activate=0,
        starting_phase=TrainingPhase.FAMILIARIZATION,
        optional_per_progression=2,
        description="progressione -> 3 opzionale per livello"
    ),
}


    def __init__(self, chapter_configs: List[ChapterConfig], initial_policy: InitialActivationPolicy, cpt_file="chapter_dataset.json"):

        self.profile_config = self.INITIAL_PROFILES[initial_policy] # da quale difficoltà inizio
        self.current_phase = self.profile_config.starting_phase # in quale fase di training mi trovo
        self.phase_iteration_count = 0 # numero iterazioni dall'avvio della fase corrente
        # Dizionario per tutti i capitoli
        # Carica e inietta CPT prima di costruire tutto il resto
        self.configs = {c.chapter_id: c for c in chapter_configs}
        cpts = self._load_cpts(cpt_file)
        self._inject_cpts(cpts)   # ← chiamata esplicita

        # debug post-iniezione
        for cid, cfg in self.configs.items():
            print(f"\n=== {cid} ===")
            print("Errors CPT:", cfg.cpt_errors)
            print("Time CPT:",   cfg.cpt_time)
            print("Max possible errors:", cfg.max_possible_errors)
            print("Min expected time sec:", cfg.min_expected_time_sec)
            print("Max expected time sec:", cfg.max_expected_time_sec)
            print("Mastery threshold:", cfg.mastery_threshold)
            if cfg.has_difficulty_level:
                print("Errors CPT advanced:", cfg.cpt_errors_advanced)
                print("Time CPT advanced:",   cfg.cpt_time_advanced)
                print("Max possible errors advanced:", cfg.max_possible_errors_advanced)
                print("Min expected time sec advanced:", cfg.min_expected_time_sec_advanced)
                print("Max expected time sec advanced:", cfg.max_expected_time_sec_advanced)


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

        for cfg in chapter_configs:
          if (cfg.has_difficulty_level
                  and cfg.max_possible_errors_advanced is not None
                  and cfg.min_expected_time_sec_advanced is not None
                  and cfg.max_expected_time_sec_advanced is not None):
              self.normalizer.register_chapter(
                  chapter_id=cfg.chapter_id + "_advanced",
                  max_possible_errors=cfg.max_possible_errors_advanced,
                  min_expected_time_sec=cfg.min_expected_time_sec_advanced,
                  max_expected_time_sec=cfg.max_expected_time_sec_advanced,
              )

        print("=== INIT SESSION ===")

        self._apply_initial_activation()

        for cid, state in self.configs.items():
            print(cid, "mandatory:", state.is_mandatory,
                "active:", self.chapter_states[cid].is_active)

        print("OPTIONAL IDS:", self.optional_ids)

        # ── Nuovi contatori globali ──────────────────────────────────────
        self._momentum_counter: int = 0
        self._add_momentum: int = self.profile_config.optional_per_progression # quanti opzionali aggiungere al prossimo trigger
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

        # ── Per debug (quali capitoli sono andati bene e quali male) ──────────────────────────────────────
        self._good_chapters_this_iter: Dict[str, bool] = {}
        self._struggling_chapters_this_iter: Dict[str, bool] = {}

    def _apply_initial_activation(self):

        # Attiva sempre obbligatori
        for cid, state in self.configs.items():
            if state.is_mandatory:
                self.chapter_states[cid].is_active = True
            else:
                self.chapter_states[cid].is_active = False

        for cid in self.optional_ids[:self.profile_config.optional_to_activate]:
            self.chapter_states[cid].is_active = True
            self._activate_next_optional(phase_config= self.PHASE_CONFIGS[self.profile_config.starting_phase])

        # Imposta fase iniziale
        self.current_phase = self.profile_config.starting_phase
        print(f"=== INITIAL ACTIVATION POLICY: {self.profile_config.name} ===")
        print(f"=== TRAINING PHASE: {self.current_phase} ===")

    def _check_phase_transition(self, phase_config: PhaseConfig):

        if self.phase_iteration_count >= phase_config.iteration_count:
            self._advance_phase()

    def _advance_phase(self):

        old_phase = self.current_phase

        if self.current_phase == TrainingPhase.FAMILIARIZATION:
            self.current_phase = TrainingPhase.AUTOMATION

        self.phase_iteration_count = 0

        print(f"[PHASE] Transition: {old_phase} → {self.current_phase}")

    def compute_alpha(self,n: int) -> float:
        if n == 2:
            return 0.2
        elif n == 3:
            return 0.5
        else:
            return 1.0

    def _load_cpts(self, filename="chapter_dataset.json") -> dict:
        try:
            base_dir = Path(__file__).parent
        except NameError:
            base_dir = Path.cwd()
        path = base_dir / filename
        with open(path, "r") as f:
            return json.load(f)

    def _inject_cpts(self, cpts: dict):
        for cid, cfg in self.configs.items():
            data = cpts.get(cid)
            if not data:
                continue

            # CPT base
            cfg.cpt_errors = data.get("cpt_errors")
            cfg.cpt_time   = data.get("cpt_time")

            # parametri normalizzatore base
            norm = data.get("normalizer")
            if norm:
                cfg.max_possible_errors    = norm["max_possible_errors"]
                cfg.min_expected_time_sec  = norm["min_expected_time_sec"]
                cfg.max_expected_time_sec  = norm["max_expected_time_sec"]

            # CPT e normalizzatore avanzati — solo se il capitolo li ha
            if cfg.has_difficulty_level:
                cfg.cpt_errors_advanced = data.get("cpt_errors_advanced")
                cfg.cpt_time_advanced   = data.get("cpt_time_advanced")

                norm_adv = data.get("normalizer_advanced")
                if norm_adv:
                    cfg.max_possible_errors_advanced   = norm_adv["max_possible_errors"]
                    cfg.min_expected_time_sec_advanced = norm_adv["min_expected_time_sec"]
                    cfg.max_expected_time_sec_advanced = norm_adv["max_expected_time_sec"]
            # solgia mastery
            cfg.mastery_threshold = data.get("mastery")

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

        # ===== NUOVO: Normalizzazione =====
        difficulty = self.chapter_states[chapter_id].difficulty_version
        norm_id = chapter_id + "_advanced" if (
            difficulty == 1
            and self.configs[chapter_id].has_difficulty_level
            and (chapter_id + "_advanced") in self.normalizer.profiles
        ) else chapter_id

        metrics = self.normalizer.normalize(norm_id, errors, time_sec)
        error_bin = metrics.error_bin
        time_bin = metrics.time_bin

        # ── 2. Aggiornamento Bayesiano sequenziale ───────────────────────
        # Alla prima iterazione usa la prior di default (None).
        # Dalle iterazioni successive usa la posterior precedente come
        # nuova prior, implementando l'aggiornamento sequenziale.

        # Smoothing della prior verso la distribuzione originale
        state.iteration_count += 1 # contatore iter per capitolo
        beta = self.compute_alpha(state.iteration_count) # peso della prior con memoria rispetto a quella di default
        original_prior = PRIOR_SKILL # prior default
        prev = state.skill_posterior # prior con memoria

        MIN_PRIOR = 0.01  # nessuna skill scende sotto l'1%
        smoothed_prior = [
            max(beta * prev[i] + (1 - beta) * original_prior[i], MIN_PRIOR)
            for i in range(3)
        ]
        # rinormalizza
        total = sum(smoothed_prior)
        smoothed_prior = [p / total for p in smoothed_prior]

        print("\n========== SMOOTHING DEBUG ==========")
        print("beta:", beta)
        print("prev:", prev)
        print("original_prior:", original_prior)
        print("smoothed_prior:", smoothed_prior)

        # La BN usa la prior attenuata
        new_posterior = bn.infer_skill(error_bin, time_bin, prior_override=smoothed_prior)
        skill_score = AdaptiveDecisionEngine.compute_skill_score(new_posterior)

        print("\n========== CHAPTER DEBUG ==========")
        print(f"[{chapter_id}] {chapter_name}")

        print("PRIOR USED:")
        print([round(p, 3) for p in smoothed_prior])

        print("POSTERIOR:")
        print([round(p, 3) for p in new_posterior])

        print("SKILL SCORE:")
        print(round(skill_score, 4))

        print("FEEBACKLEVEL: ", state.feedback_level)

        state.skill_posterior = new_posterior
        state.observations.append((errors, time_sec))
        state.seen_this_iter = True

        # ── 3. Feedback locale ───────────────────────────────────────────
        # Il feedback dipende solo dalla posterior di questo capitolo.

        # Aggiorna il flag di difficoltà per questo capitolo
        is_struggling = AdaptiveDecisionEngine.is_struggling(skill_score)

        if is_struggling:
            self._had_struggle_this_iter[chapter_id] = True

        had_struggle = self._had_struggle_this_iter.get(chapter_id, False)

        old_feedback = state.feedback_level
        new_feedback = AdaptiveDecisionEngine.determine_feedback_level(
            new_posterior, old_feedback, had_struggle, phase_config= self.PHASE_CONFIGS[self.current_phase]
        )
        state.feedback_level = new_feedback


        # ── 4. Registra risultato nell'iterazione corrente ───────────────
        current_result = AdaptiveDecisionEngine.is_good(skill_score)

        self._good_chapters_this_iter[chapter_id] = current_result

        self._struggling_chapters_this_iter[chapter_id] = (
            AdaptiveDecisionEngine.is_struggling(skill_score)
        )

        if chapter_id in self._current_iteration_results:
            # Se il capitolo è già stato osservato in questa iterazione
            # (non dovrebbe succedere con l'uso normale, ma gestiamo il caso),
            # prendiamo il risultato più conservativo (AND logico).
            self._current_iteration_results[chapter_id] = (
                self._current_iteration_results[chapter_id] and current_result
            )
        else:
            self._current_iteration_results[chapter_id] = current_result

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

        add_optional    = True
        remove_optional = False
        removed_chapter_id = None
        added_chapter_ids = []
        upgraded_chapter_ids = []

        if iteration_complete:

            scores = [
              AdaptiveDecisionEngine.compute_skill_score(self.chapter_states[cid].skill_posterior)
              for cid in active_chapters]

            avg_score = sum(scores) / len(scores)

            if avg_score >= 0.75:   # zona ottima
                self._momentum_counter += 2
                print(f"MOMENTUM CHANGE: + {self._momentum_counter}/2")
            elif avg_score >= 0.45:      # zona buona
                self._momentum_counter += 1
                print(f"MOMENTUM CHANGE: + {self._momentum_counter}/2")
            elif avg_score < 0.15 and self.current_phase == TrainingPhase.FAMILIARIZATION:
                self._momentum_counter = 0
                self._add_momentum = max(0, self._add_momentum - 1)
                print(f"MOMENTUM CHANGE: - 1")
            elif self.current_phase == TrainingPhase.AUTOMATION:     # zona difficoltà
                self._momentum_counter = 0
                self._add_momentum = max(0, self._add_momentum - 1)
                print(f"MOMENTUM CHANGE: - 1")
            
            while self._momentum_counter >= 2:
                self._add_momentum += 1
                self._momentum_counter -= 2
            self._add_momentum = min(self._add_momentum, 2)

            # Aggiunta base
            base_added = self._activate_next_optional(
                phase_config=self.PHASE_CONFIGS[self.current_phase]
            )

            if base_added:
                added_chapter_ids.append(base_added)

            # fallback se opzionali finiti
            else:
                upgraded = self._activate_next_difficulty(
                    phase_config=self.PHASE_CONFIGS[self.current_phase]
                )
                if upgraded:
                    upgraded_chapter_ids.append(upgraded)

            # Aggiunta extra
            for _ in range(self._add_momentum):
                activated = self._activate_next_optional(phase_config= self.PHASE_CONFIGS[self.current_phase])
                if activated:
                    add_optional = True
                    added_chapter_ids.append(activated)
                else:
                    # Opzionali esauriti → prova versione advance
                    upgraded = self._activate_next_difficulty(
                        phase_config=self.PHASE_CONFIGS[self.current_phase]
                    )
                    if upgraded:
                        upgraded_chapter_ids.append(upgraded)
                    else:
                        break  # niente più da aggiungere

            good_chapters = [
                cid for cid, v in self._good_chapters_this_iter.items() if v
            ]

            struggling_chapters = [
                cid for cid, v in self._struggling_chapters_this_iter.items() if v
            ]

            print("\n========== ITERATION QUALITY DEBUG ==========")
            print("GOOD chapters:", good_chapters)
            print("STRUGGLING chapters:", struggling_chapters)
            print("ADD MOMENTUM: ", self._add_momentum)
            print("AVG_SKILL: ", avg_score)
            print("\n=============================================")
            # Reset per la prossima iterazione
            self._current_iteration_results = {}
            self._good_chapters_this_iter = {}
            self._struggling_chapters_this_iter = {}
            # Reset del flag di difficoltà locale per la prossima iterazione
            self._had_struggle_this_iter = {cid: False for cid in self.configs}
            self._early_remove_done_this_iter = False
            self.phase_iteration_count += 1
            self.reset_seen_chapters()
            self._check_phase_transition(self.PHASE_CONFIGS[self.current_phase])

        early_remove = False  # sempre inizializzata

        # if not iteration_complete:
        #   if not self._early_remove_done_this_iter:
        #     early_remove = self._should_remove_optional_early(phase_config = self.PHASE_CONFIGS[self.current_phase])

        # # Rimozione anticipata
        # if early_remove:
        #     removed = self._deactivate_weakest_optional(phase_config = self.PHASE_CONFIGS[self.current_phase])
        #     if removed:
        #         remove_optional = True
        #         removed_chapter_id = removed
        #         self._early_remove_done_this_iter = True


        # ── 6. Verifica padronanza del singolo capitolo ──────────────────
        # Un capitolo è padroneggiato se P(Expert) supera MASTERY_THRESHOLD.
        # Non usiamo isteresi qui perché la padronanza è una proprietà
        # del singolo capitolo, non dell'iterazione completa.


        old_difficulty = state.difficulty_version
        new_difficulty = state.difficulty_version  # Default: no change

        chapter_mastered = (
            new_posterior[SkillLevel.EXPERT.value]
            >  self.configs[chapter_id].mastery_threshold
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
            add_optional, added_chapter_ids, upgraded_chapter_ids, remove_optional, early_remove, chapter_mastered, iteration_complete
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
            added_chapter_ids = added_chapter_ids,
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
            print("=== La fase corrente non ammette l'aggiunta di opzionali.")
            return None

        # Lista A: never_shown nell'ordine originale
        never_shown = [
            cid for cid in self.optional_ids
            if self.chapter_states[cid].optional_status == OptionalStatus.NEVER_SHOWN
        ]
        if never_shown:
            chosen = never_shown[0]  # ordine originale preservato
            print(f"AVVISO: selezionato l'opzionale non visto : {chosen}")
            self.chapter_states[chosen].is_active = True
            self.chapter_states[chosen].optional_status = OptionalStatus.ACTIVE
            return chosen
        else:
            print("AVVISO: non ci sono opzionali non visti")

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

    def _activate_next_difficulty(self, phase_config: PhaseConfig) -> Optional[str]:
      if not phase_config.allow_optional:
          return None

      eligible = [
          cid for cid in self.configs
          if (
              self.configs[cid].has_difficulty_level
              and self.chapter_states[cid].is_active
              and self.chapter_states[cid].iteration_count > 0
              and self.chapter_states[cid].difficulty_version == 0
          )
      ]
      if not eligible:
          return None

      chosen = max(
          eligible,
          key=lambda cid: AdaptiveDecisionEngine.compute_skill_score(
              self.chapter_states[cid].skill_posterior
          )
      )
      self.chapter_states[chosen].difficulty_version = 1
      self.chapter_states[chosen].feedback_level = 0
      self.chapter_bns[chosen].switch_to_advanced()  # ← nuovo
      print(f"[DIFFICULTY UP] {chosen} → versione avanzata")
      return chosen

    def _deactivate_weakest_optional(self, phase_config: PhaseConfig) -> Optional[str]:
        """
        Rimuove l'opzionale con P(Expert) massima tra quelli attivi
        e non ancora padroneggiati.
        """
        if not phase_config.allow_optional:
            print("=== La fase corrente non ammette l'upgrade della difficoltà.")
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
            print("=== La fase corrente non ammette rimozione anticipata.")
            return False

        # procedi solo se ho eseguito almeno la metà dei capitoli totali attivi
        active_chapters = [cid for cid in self.configs if self.chapter_states[cid].is_active]
        completed_chapters = [cid for cid in active_chapters if cid in self._current_iteration_results]

        if len(active_chapters) == 0:
            return False

        if len(completed_chapters) == 0:
          return False

        if len(completed_chapters) / len(active_chapters) \
                < self.EARLY_REMOVE_MIN_FRACTION_COMPLETED:
                return False # non ho abbsatnza dati

        # controlla se sono struggle
        struggling = [
            cid for cid in completed_chapters
            if AdaptiveDecisionEngine.is_struggling(AdaptiveDecisionEngine.compute_skill_score(self.chapter_states[cid].skill_posterior))
        ]

        return (
            len(struggling) / len(completed_chapters)
            > self.EARLY_REMOVE_STRUGGLE_FRACTION
        )

    def _build_message(
        self, chapter_id, chapter_name, posterior, new_fb, old_fb, old_dl, new_dl,
        add_opt, added_chapter_ids, upgraded_chapter_ids, rem_opt, early_rem, mastered, iter_complete
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
            parts.append(f"Difficulty level: {dl_desc[old_dl]} → {dl_desc[new_dl]}")
        if mastered:
            parts.append("✓ Capitolo padroneggiato")
        if iter_complete:
            parts.append(
                f"[iter completa | "
                f"buoni_glob={self._consecutive_good_global} "
                f"diff_glob={self._consecutive_struggle_global}]"
            )
        if add_opt and added_chapter_ids:
            parts.append(f"+ Aggiunti opzionali: {', '.join(added_chapter_ids)}")
        if upgraded_chapter_ids:
            parts.append(f"↑ Versione avanzata attivata: {', '.join(upgraded_chapter_ids)}")
        if rem_opt:
            if early_rem:
              parts.append("- Rimosso capitolo opzionale per early_remove")
            else:
              parts.append("- Rimosso capitolo opzionale")
        return " | ".join(parts)