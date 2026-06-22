# -*- coding: utf-8 -*-
"""
Modulo di normalizzazione adattiva per il sistema antincendio
=============================================================

Questo modulo risolve il problema della discretizzazione ingiusta tra capitoli
di lunghezza/complessità diverse.

Meccanismo:
1. Ogni capitolo ha un profilo di "complessità baseline" (quanti errori massimi
   sono teoricamente possibili, quanto tempo atteso)
2. Le metriche grezze (errori assoluti, secondi) vengono normalizzate in
   rapporti relativi: error_rate, time_efficiency
3. La BN riceve sempre metriche normalizzate in [0, 1], indipendenti dal capitolo

Uso:
    normalizer = ChapterNormalizer()
    normalizer.register_chapter("cap1", max_errors=10, expected_time_sec=300)
    
    # Al termine di un capitolo:
    metrics = normalizer.normalize("cap1", errors=5, time_sec=240)
    # returns: {"error_rate": 0.5, "time_efficiency": 0.8, ...}
"""

import numpy as np
from dataclasses import dataclass, field
from typing import Dict, Optional, Tuple
from enum import Enum


class ErrorBin(Enum):
    LOW = 0
    MEDIUM = 1
    HIGH = 2


class TimeBin(Enum):
    SHORT = 0
    MEDIUM = 1
    LONG = 2

# per la discretizzazione fissa (VECCHIA VERSIONE)
ERROR_THRESHOLDS = (3, 6)   # (soglia_basso_medio, soglia_medio_alto)
TIME_THRESHOLDS = (60.0, 300) # minuti (soglia_breve_medio, soglia_medio_lungo)


@dataclass
class ChapterComplexityProfile:
    """
    Profilo di complessità di un capitolo.
    
    Attributes:
        chapter_id: Identificatore unico
        max_possible_errors: Massimo numero di errori teoricamente commettibili
        min_expected_time_sec: Tempo minimo per completare (utente esperto)
        max_expected_time_sec: Tempo massimo accettabile (utente novice)
        percentile_threshold_low: Percentile per discretizzazione basso/medio
        percentile_threshold_high: Percentile per discretizzazione medio/alto
    """
    chapter_id: str
    max_possible_errors: int
    min_expected_time_sec: float
    max_expected_time_sec: float
    # Inizialmente usiamo quartili fissi, poi adattiamo ai dati empirici
    percentile_threshold_low: float = 33.33
    percentile_threshold_high: float = 66.66
    
    # Storico osservazioni per auto-adattamento
    historical_errors: list = field(default_factory=list)
    historical_times: list = field(default_factory=list)

    use_adaptive_thresholds: bool = False


@dataclass
class NormalizedMetrics:
    """Output della normalizzazione."""
    error_rate: float        # [0, 1] - rapporto errori / max possibili
    time_efficiency: float   # [0, 1] - 1 = veloce, 0 = lento
    time_rate: float         # [0, 1] - rapporto tempo vs range atteso
    
    # Versioni discretizzate (utili per debug)
    error_bin: ErrorBin
    time_bin: TimeBin
    
    # Metadati
    chapter_id: str
    raw_errors: int
    raw_time_sec: float
    

class ChapterNormalizer:
    """
    Gestore della normalizzazione adattiva per i capitoli.
    
    Mantiene profili di complessità per ogni capitolo e offre due modi di
    discretizzazione:
    
    1. THRESHOLD-BASED (default): Usi soglie fisse. Adatto a inizio.
    2. PERCENTILE-BASED: Adatta le soglie ai dati storici. Attiva dopo N osservazioni.
    """
    
    # Numero minimo di osservazioni prima di passare a percentile-based
    ADAPTIVE_THRESHOLD_MIN_OBSERVATIONS = 5
    
    def __init__(self):
        self.profiles: Dict[str, ChapterComplexityProfile] = {}
        ##self.use_adaptive_thresholds = False
    
    def register_chapter(
        self,
        chapter_id: str,
        max_possible_errors: int,
        min_expected_time_sec: float,
        max_expected_time_sec: float
    ) -> None:
        """
        Registra un capitolo con i suoi parametri di complessità.
        
        Args:
            chapter_id: ID univoco
            max_possible_errors: Massimo numero di errori commettibili
                (es: cap1="riconoscimento allarme" → max 2 errori possibili
                     cap4="uso estintore" → max 15 errori possibili)
            min_expected_time_sec: Tempo minimo (utente esperto)
            max_expected_time_sec: Tempo massimo (utente novice)
        """
        self.profiles[chapter_id] = ChapterComplexityProfile(
            chapter_id=chapter_id,
            max_possible_errors=max_possible_errors,
            min_expected_time_sec=min_expected_time_sec,
            max_expected_time_sec=max_expected_time_sec,
        )
    
    def normalize(
        self,
        chapter_id: str,
        errors: int,
        time_sec: float
    ) -> NormalizedMetrics:
        """
        Normalizza le metriche grezze di un capitolo.
        
        Args:
            chapter_id: ID del capitolo
            errors: Numero di errori commessi (assoluto)
            time_sec: Tempo impiegato in secondi (assoluto)
        
        Returns:
            NormalizedMetrics con valori in [0, 1] e bin discretizzati
        
        Raises:
            KeyError: Se il capitolo non è stato registrato
        """
        if chapter_id not in self.profiles:
            raise KeyError(
                f"Capitolo {chapter_id} non registrato. "
                f"Registra con register_chapter() prima."
            )
        
        profile = self.profiles[chapter_id]
        
        # === NORMALIZZAZIONE ERRORI ===
        # error_rate = errori osservati / massimi possibili
        # Clamp a [0, 1] perché l'utente potrebbe teoricamente sbagliare
        # più volte lo stesso errore
        error_rate = min(errors / profile.max_possible_errors, 1.0)
        
        # === NORMALIZZAZIONE TEMPO ===
        # time_rate = (tempo_osservato - min) / (max - min)
        # Clamp a [0, 1]
        time_range = profile.max_expected_time_sec - profile.min_expected_time_sec
        if time_range > 0:
            time_rate = (time_sec - profile.min_expected_time_sec) / time_range
        else:
            time_rate = 0.5  # fallback se min == max
        
        time_rate = np.clip(time_rate, 0.0, 1.0)
        
        # time_efficiency = quanto è stato veloce (1 = velocissimo, 0 = lentissimo)
        time_efficiency = 1.0 - time_rate
        
        # === DISCRETIZZAZIONE ===
        error_bin = self._discretize_error(chapter_id, error_rate)
        time_bin = self._discretize_time(chapter_id, time_efficiency)
        
        # Aggiorna storico per adattamento futuro
        profile.historical_errors.append(error_rate)
        profile.historical_times.append(time_efficiency)
        
        # Attiva soglie adattive se abbiamo abbastanza dati
        if (len(profile.historical_errors) >= self.ADAPTIVE_THRESHOLD_MIN_OBSERVATIONS
            and not profile.use_adaptive_thresholds):
            ##self.use_adaptive_thresholds = True
            profile.use_adaptive_thresholds = True
            print(f"[NORMALIZER] Capitolo {chapter_id}: soglie adattive attivate")
        
        return NormalizedMetrics(
            error_rate=error_rate,
            time_efficiency=time_efficiency,
            time_rate=time_rate,
            error_bin=error_bin,
            time_bin=time_bin,
            chapter_id=chapter_id,
            raw_errors=errors,
            raw_time_sec=time_sec,
        )
    
    def _discretize_error(self, chapter_id: str, error_rate: float) -> ErrorBin:
        """Converte error_rate [0,1] in bin {LOW, MEDIUM, HIGH}."""
        profile = self.profiles[chapter_id]
        
        if profile.use_adaptive_thresholds:
            # Usa percentili su storico
            thresholds = self._compute_percentile_thresholds_error(profile)
        else:
            # Usa valori fixed
            thresholds = self._compute_fixed_thresholds_error()
        
        low_th, high_th = thresholds
        
        if error_rate < low_th:
            return ErrorBin.LOW
        elif error_rate < high_th:
            return ErrorBin.MEDIUM
        else:
            return ErrorBin.HIGH
    
    def _discretize_time(self, chapter_id: str, time_efficiency: float) -> TimeBin:
        """Converte time_efficiency [0,1] in bin {SHORT, MEDIUM, LONG}."""
        profile = self.profiles[chapter_id]
        
        if profile.use_adaptive_thresholds:
            # Usa percentili su storico
            thresholds = self._compute_percentile_thresholds_time(profile)
        else:
            # Usa valori fixed
            thresholds = self._compute_fixed_thresholds_time()
        
        low_th, high_th = thresholds
        
        if time_efficiency > high_th:
            return TimeBin.SHORT      # veloce
        elif time_efficiency > low_th:
            return TimeBin.MEDIUM
        else:
            return TimeBin.LONG       # lento
    
    @staticmethod
    def _compute_fixed_thresholds_error() -> Tuple[float, float]:
        """
        Soglie fisse per error_rate in [0, 1].
        
        Basate su un modello di "utente tipico" con moderate difficulty:
        - LOW: 0-25% errori rispetto al massimo
        - MEDIUM: 25-60%
        - HIGH: 60-100%
        """
        return (0.25, 0.60)
    
    @staticmethod
    def _compute_fixed_thresholds_time() -> Tuple[float, float]:
        """
        Soglie fisse per time_efficiency in [0, 1].
        
        - SHORT: 60-100% (veloce)
        - MEDIUM: 30-60%
        - LONG: 0-30% (lento)
        """
        return (0.30, 0.60)
    
    @staticmethod
    def _compute_percentile_thresholds_error(
        profile: ChapterComplexityProfile
    ) -> Tuple[float, float]:
        """
        Soglie adattive per error_rate basate sul 33° e 66° percentile
        dello storico di questo capitolo specifico.
        """
        if len(profile.historical_errors) < 2:
            return ChapterNormalizer._compute_fixed_thresholds_error()
        
        if np.std(profile.historical_errors) < 1e-4:
            print("[NORMALIZER] Varianza dell'errore troppo piccola per le soglie adattive")
            return ChapterNormalizer._compute_fixed_thresholds_error()
        
        low_th = np.percentile(
            profile.historical_errors,
            profile.percentile_threshold_low
        )
        high_th = np.percentile(
            profile.historical_errors,
            profile.percentile_threshold_high
        )
        
        return (low_th, high_th)
    
    @staticmethod
    def _compute_percentile_thresholds_time(
        profile: ChapterComplexityProfile
    ) -> Tuple[float, float]:
        """
        Soglie adattive per time_efficiency basate sul 33° e 66° percentile
        dello storico di questo capitolo specifico.
        """
        if len(profile.historical_times) < 2:
            return ChapterNormalizer._compute_fixed_thresholds_time()
        
        if np.std(profile.historical_times) < 1e-4:
            print("[NORMALIZER] Varianza dei tempi troppo piccola per le soglie adattive")
            return ChapterNormalizer._compute_fixed_thresholds_time()
        
        low_th = np.percentile(
            profile.historical_times,
            profile.percentile_threshold_low
        )
        high_th = np.percentile(
            profile.historical_times,
            profile.percentile_threshold_high
        )
        
        return (low_th, high_th)
    
    def get_profile_summary(self, chapter_id: str) -> Dict:
        """Debug: visualizza il profilo di un capitolo."""
        if chapter_id not in self.profiles:
            return {}
        
        profile = self.profiles[chapter_id]
        return {
            "chapter_id": chapter_id,
            "max_errors": profile.max_possible_errors,
            "time_range_sec": (
                profile.min_expected_time_sec,
                profile.max_expected_time_sec
            ),
            "num_observations": len(profile.historical_errors),
            "historical_error_rates": profile.historical_errors,
            "historical_time_efficiencies": profile.historical_times,
            "use_adaptive_thresholds": profile.use_adaptive_thresholds
        }

    def fixed_discretize_errors(error_count: int) -> ErrorBin:
        """
        Converte un conteggio assoluto di errori nel bin corrispondente.
        Modifica ERROR_THRESHOLDS in base al tuo dominio.
        """
        low_thresh, high_thresh = ERROR_THRESHOLDS
        if error_count <= low_thresh:
            print("Low")
            return ErrorBin.LOW
        elif error_count <= high_thresh:
            print("Medium (error)")
            return ErrorBin.MEDIUM
        else:
            print("High")
            return ErrorBin.HIGH

    def fixed_discretize_time(time_seconds: float) -> TimeBin:
        """
        Converte un tempo in minuti nel bin corrispondente.
        Modifica TIME_THRESHOLDS in base al tuo dominio.
        """
        short_thresh, long_thresh = TIME_THRESHOLDS
        if time_seconds < short_thresh:
            print(f"Short: {time_seconds}")
            return TimeBin.SHORT
        elif time_seconds <= long_thresh:
            print(f"Medium: {time_seconds}")
            return TimeBin.MEDIUM
        else:
            print(f"Long: {time_seconds}")
            return TimeBin.LONG

    def discretize_fallback(self, errors: int, time_sec: float) -> Tuple[ErrorBin, TimeBin]:
        # usa ERROR_THRESHOLDS e TIME_THRESHOLDS
        error_bin = self.fixed_discretize_errors(errors)
        time_bin = self.fixed_discretize_time(time_sec)
        return error_bin, time_bin

