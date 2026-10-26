using UnityEngine;

/// <summary>
/// Generador de sonidos procedurales de respaldo y utilidades de reproducción para el Invernadero (Escena 5) y la voz de Vega en toda la nave.
/// Si el usuario arrastra sus propios clips en los AudioSource del Inspector (o en las casillas de los scripts),
/// siempre tienen prioridad absoluta sobre los sonidos procedurales.
/// </summary>
public static class INV_SoundHelper
{
    private const int SampleRate = 22050;

    private static AudioClip cachedHeatHum;
    private static AudioClip cachedDarkClick;
    private static AudioClip cachedStepPulse;
    private static AudioClip cachedMistHiss;
    private static AudioClip cachedBloomHarmonic;
    private static AudioClip cachedAlarmSiren;
    private static AudioClip cachedPerillaClick;
    private static AudioClip cachedTuningComplete;
    private static AudioClip cachedGreenhouseAmbience;
    private static AudioClip cachedSporeWither;
    private static AudioClip cachedPaperRustle;
    private static AudioClip cachedAshInspect;
    private static AudioClip cachedWindowWind;
    private static AudioClip cachedCartDeploy;
    private static AudioClip cachedDoorAirlock;
    private static readonly AudioClip[] cachedVegaTransmissions = new AudioClip[6];
    private static AudioClip cachedVegaIntercomOpen;
    private static AudioClip cachedVegaIntercomClose;

    // =========================================================================
    // UTILIDADES DE REPRODUCCIÓN (RESPETAN EL INSPECTOR DEL USUARIO)
    // =========================================================================

    /// <summary>Indica si un AudioSource o su override ya tienen un audio asignado por el usuario en el Inspector.</summary>
    public static bool HasUserAssignedAudio(AudioSource source, AudioClip scriptOverride = null)
    {
        if (scriptOverride != null) return true;
        if (source == null) return false;
        return source.clip != null || source.resource != null;
    }

    /// <summary>
    /// Reproduce un sonido puntual dando prioridad a:
    /// 1) scriptOverride asignado en el script,
    /// 2) AudioClip / AudioResource asignado directamente en el componente AudioSource del Inspector,
    /// 3) proceduralFallback si el usuario aún no ha asignado ningún archivo de audio.
    /// </summary>
    public static void PlayEffect(
        AudioSource source,
        AudioClip scriptOverride,
        AudioClip proceduralFallback,
        float fallbackVolumeScale = 0.85f)
    {
        if (source == null) return;

        if (scriptOverride != null)
        {
            source.PlayOneShot(scriptOverride);
            return;
        }

        if (source.clip != null)
        {
            source.PlayOneShot(source.clip);
            return;
        }

        if (source.resource != null)
        {
            source.Stop();
            source.loop = false;
            source.Play();
            return;
        }

        if (proceduralFallback != null)
        {
            source.PlayOneShot(proceduralFallback, fallbackVolumeScale);
        }
    }

    /// <summary>
    /// Inicia un AudioSource en bucle (Loop) respetando el clip asignado en el Inspector
    /// o usando el fallback procedural si aún está vacío.
    /// </summary>
    public static void PlayLoop(
        AudioSource source,
        AudioClip scriptOverride,
        AudioClip proceduralFallback,
        float fallbackVolume = 0.35f)
    {
        if (source == null) return;

        source.loop = true;

        if (scriptOverride != null)
        {
            if (source.clip != scriptOverride)
            {
                source.Stop();
                source.clip = scriptOverride;
            }
            if (!source.isPlaying) source.Play();
            return;
        }

        if (source.clip != null || source.resource != null)
        {
            if (!source.isPlaying) source.Play();
            return;
        }

        if (proceduralFallback != null)
        {
            source.clip = proceduralFallback;
            source.volume = fallbackVolume;
            if (!source.isPlaying) source.Play();
        }
    }

    /// <summary>Busca un AudioSource en un hijo por nombre o lo crea limpio en 2D si no existiera.</summary>
    public static AudioSource FindOrCreateChildSource(
        Transform parent,
        string childName,
        float defaultVolume = 0.8f,
        bool loop = false)
    {
        if (parent == null) return null;

        Transform child = parent.Find(childName);
        if (child != null && child.TryGetComponent(out AudioSource existing))
        {
            existing.playOnAwake = false;
            return existing;
        }

        GameObject go = child != null ? child.gameObject : new GameObject(childName);
        if (child == null)
        {
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
        }

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null) src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        src.volume = defaultVolume;
        return src;
    }

    // =========================================================================
    // SONIDOS PROCEDURALES CACHEADOS (FALLBACKS AUTOMÁTICOS)
    // =========================================================================

    public static AudioClip CreateHeatHum() => cachedHeatHum ??= GenerateClip("INV_HumCalor", 1.8f, (t, i) =>
    {
        float env = Mathf.Min(t * 4f, 1f) * Mathf.Clamp01((1.8f - t) * 2f);
        float s1 = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.35f;
        float s2 = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.2f;
        float buzz = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.3f;
        return (s1 + s2 + buzz) * env * 0.45f;
    });

    public static AudioClip CreateDarkClick() => cachedDarkClick ??= GenerateClip("INV_ClicOscuro", 0.9f, (t, i) =>
    {
        float env = Mathf.Exp(-t * 8f);
        float s1 = Mathf.Sin(2f * Mathf.PI * 75f * (1f - t * 0.5f) * t);
        float click = Mathf.Sin(2f * Mathf.PI * 340f * t) * Mathf.Exp(-t * 35f) * 0.6f;
        return (s1 * 0.6f + click) * env * 0.55f;
    });

    public static AudioClip CreateStepPulse() => cachedStepPulse ??= GenerateClip("INV_PulsoPasos", 1.2f, (t, i) =>
    {
        float beat1 = (t < 0.55f) ? Mathf.Sin(2f * Mathf.PI * 82f * t) * Mathf.Exp(-t * 9f) : 0f;
        float t2 = t - 0.55f;
        float beat2 = (t2 >= 0f) ? Mathf.Sin(2f * Mathf.PI * 74f * t2) * Mathf.Exp(-t2 * 9f) : 0f;
        return (beat1 + beat2 * 0.85f) * 0.5f;
    });

    public static AudioClip CreateMistHiss()
    {
        if (cachedMistHiss != null) return cachedMistHiss;
        var rnd = new System.Random(42);
        cachedMistHiss = GenerateClip("INV_SiseoBruma", 2.0f, (t, i) =>
        {
            float env = Mathf.Min(t * 3f, 1f) * Mathf.Clamp01((2.0f - t) * 2f);
            float noise = ((float)rnd.NextDouble() * 2f - 1f) * 0.25f;
            float gentleWhistle = Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.04f;
            return (noise + gentleWhistle) * env * 0.5f;
        });
        return cachedMistHiss;
    }

    public static AudioClip CreateBloomHarmonic()
    {
        if (cachedBloomHarmonic != null) return cachedBloomHarmonic;
        float[] notes = { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f };
        cachedBloomHarmonic = GenerateClip("INV_FloracionArmonica", 5.0f, (t, i) =>
        {
            float env = Mathf.Min(t * 1.5f, 1f) * Mathf.Clamp01((5.0f - t) * 0.5f);
            float sample = 0f;
            for (int n = 0; n < notes.Length; n++)
            {
                float delay = n * 0.45f;
                if (t >= delay)
                {
                    float noteT = t - delay;
                    sample += Mathf.Sin(2f * Mathf.PI * notes[n] * noteT) * Mathf.Exp(-noteT * 0.7f);
                }
            }
            return sample * env * 0.22f;
        });
        return cachedBloomHarmonic;
    }

    public static AudioClip CreateAlarmSiren() => cachedAlarmSiren ??= GenerateClip("INV_AlarmaEsporas_15s", 15.0f, (t, i) =>
    {
        float fadeOut = Mathf.Clamp01((15.0f - t) / 0.85f);
        float pulse = Mathf.Floor((t % 0.8f) / 0.4f) == 0 ? 1f : 0f;
        float pitch = 580f + Mathf.Sin(2f * Mathf.PI * 4f * t) * 90f;
        return Mathf.Sin(2f * Mathf.PI * pitch * t) * pulse * fadeOut * 0.25f;
    });

    public static AudioClip CreatePerillaClick() => cachedPerillaClick ??= GenerateClip("INV_PerillaClick", 0.08f, (t, i) =>
    {
        float env = Mathf.Exp(-t * 70f);
        return Mathf.Sin(2f * Mathf.PI * 920f * t) * env * 0.35f;
    });

    /// <summary>Confirmación armónica al estabilizar los 3 segundos de afinación (2-4-1-3).</summary>
    public static AudioClip CreateTuningCompleteChime() => cachedTuningComplete ??= GenerateClip("INV_AfinacionOk", 0.85f, (t, i) =>
    {
        float n1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * Mathf.Exp(-t * 5.5f);
        float n2 = (t >= 0.14f) ? Mathf.Sin(2f * Mathf.PI * 783.99f * (t - 0.14f)) * Mathf.Exp(-(t - 0.14f) * 3.8f) : 0f;
        return (n1 * 0.5f + n2 * 0.65f) * 0.42f;
    });

    /// <summary>
    /// Transmisión de radio / voz sintetizada de Vega cuando habla por el intercomunicador de la nave.
    /// Incluye pitido sci-fi de canal abierto, leve estática de radio, cadencia vocal cálida y cierre de transmisión.
    /// Variantes:
    /// 0 = Súplica suave / reflexiva (A4, A3, A1 relectura)
    /// 1 = Urgencia y alarma (A2 cabina sellada, Alerta 1 Invernadero)
    /// 2 = Voz débil / falta de aire (Alerta 2, caminata final hacia la esclusa)
    /// 3 = Ruego quebrado (Alerta 3, súplica final frente a cabina)
    /// 4 = Registro clínico de bitácora (A1 al despertar en el Laboratorio)
    /// 5 = Transmisión cortada en seco en la Esclusa ("La voz de Vega se corta")
    /// </summary>
    public static AudioClip CreateVegaVoiceTransmission(int variant = 0)
    {
        int idx = Mathf.Clamp(variant, 0, cachedVegaTransmissions.Length - 1);
        if (cachedVegaTransmissions[idx] != null) return cachedVegaTransmissions[idx];

        var rnd = new System.Random(108 + idx * 37);

        // Variante 5: La última transmisión de Vega al abrir la esclusa que se corta abruptamente a mitad de frase
        if (idx == 5)
        {
            const float cutoffTime = 0.86f;
            float[] cutStarts = { 0.16f, 0.42f, 0.68f };
            float[] cutLengths = { 0.19f, 0.20f, 0.30f };
            float[] cutPitchMults = { 0.98f, 0.92f, 0.86f };
            const float cutBasePitch = 228f;

            cachedVegaTransmissions[idx] = GenerateClip("INV_VegaComms_Cutoff", 1.05f, (t, i) =>
            {
                float sample = 0f;

                // 1. Mismo pitido doble de apertura de intercomunicador (0.00s - 0.135s)
                if (t < 0.065f)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 28f) * 0.26f;
                }
                else if (t < 0.135f)
                {
                    float tp = t - 0.065f;
                    sample += Mathf.Sin(2f * Mathf.PI * 1174.66f * tp) * Mathf.Exp(-tp * 26f) * 0.24f;
                }

                // 2. Estática + voz débil de Vega hasta el instante exacto del corte (0.86s)
                if (t >= 0.08f && t < cutoffTime)
                {
                    float staticEnv = Mathf.Min((t - 0.08f) * 14f, 1f);
                    sample += ((float)rnd.NextDouble() * 2f - 1f) * 0.042f * staticEnv;

                    for (int s = 0; s < cutStarts.Length; s++)
                    {
                        float st = t - cutStarts[s];
                        float len = cutLengths[s];
                        if (st >= 0f && st <= len)
                        {
                            float norm = st / len;
                            float syllEnv = Mathf.Sin(Mathf.PI * norm) * Mathf.Clamp01(st * 35f);
                            float vibrato = Mathf.Sin(2f * Mathf.PI * 5.8f * t) * 4.2f;
                            float f0 = cutBasePitch * cutPitchMults[s] + vibrato;

                            float vFund = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.48f;
                            float vForm1 = Mathf.Sin(2f * Mathf.PI * (f0 * 2.0f) * t) * 0.28f;
                            float vForm2 = Mathf.Sin(2f * Mathf.PI * (f0 * 3.1f) * t) * 0.14f;
                            sample += (vFund + vForm1 + vForm2) * syllEnv * 0.40f;
                        }
                    }
                }
                // 3. Chasquido seco de corte de portadora (0.86s - 0.92s) y luego silencio absoluto
                else if (t >= cutoffTime && t <= cutoffTime + 0.06f)
                {
                    float tc = t - cutoffTime;
                    float snapNoise = ((float)rnd.NextDouble() * 2f - 1f) * Mathf.Exp(-tc * 95f) * 0.34f;
                    float dropTone = Mathf.Sin(2f * Mathf.PI * 410f * tc) * Mathf.Exp(-tc * 110f) * 0.25f;
                    sample += snapNoise + dropTone;
                }

                return Mathf.Clamp(sample, -0.85f, 0.85f);
            });

            return cachedVegaTransmissions[idx];
        }

        float basePitch = idx switch
        {
            0 => 248f, // Entrada A4 / A3 / A1 relectura: súplica suave o confesión
            1 => 286f, // A2 / Alerta 1: alarma y urgencia ("¡No puedes sacar eso de aquí!")
            2 => 232f, // Alerta 2 / Pasillo final: voz débil / falta de aire
            4 => 244f, // A1 inicial: registro clínico de formateo al despertar
            _ => 240f  // Alerta 3 / Cabina final: ruego quebrado ("Suéltala, Siete. Por favor...")
        };

        float[] syllableStarts = { 0.16f, 0.38f, 0.62f, 0.90f, 1.18f };
        float[] syllableLengths = { 0.17f, 0.19f, 0.21f, 0.22f, 0.30f };
        float[] pitchMults = idx switch
        {
            1 => new[] { 1.05f, 1.16f, 1.10f, 1.22f, 0.98f },
            2 => new[] { 1.00f, 0.95f, 0.91f, 0.88f, 0.83f },
            4 => new[] { 1.00f, 1.00f, 0.98f, 1.01f, 0.94f },
            _ => new[] { 1.04f, 0.98f, 1.02f, 0.92f, 0.85f }
        };

        cachedVegaTransmissions[idx] = GenerateClip($"INV_VegaComms_{idx}", 1.72f, (t, i) =>
        {
            float sample = 0f;

            // 1. Pitido doble de apertura de intercomunicador (0.00s - 0.13s)
            if (t < 0.065f)
            {
                sample += Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 28f) * 0.28f;
            }
            else if (t < 0.135f)
            {
                float tp = t - 0.065f;
                sample += Mathf.Sin(2f * Mathf.PI * 1174.66f * tp) * Mathf.Exp(-tp * 26f) * 0.26f;
            }

            // 2. Cama sutil de estática de radio mientras el canal está abierto (0.08s - 1.58s)
            if (t >= 0.08f && t <= 1.58f)
            {
                float staticEnv = Mathf.Min((t - 0.08f) * 14f, 1f) * Mathf.Clamp01((1.58f - t) * 12f);
                float radioNoise = ((float)rnd.NextDouble() * 2f - 1f) * 0.038f * staticEnv;
                sample += radioNoise;
            }

            // 3. Cadencia vocal cálida con formantes de intercomunicador
            for (int s = 0; s < syllableStarts.Length; s++)
            {
                float st = t - syllableStarts[s];
                float len = syllableLengths[s];
                if (st >= 0f && st <= len)
                {
                    float norm = st / len;
                    float syllEnv = Mathf.Sin(Mathf.PI * norm) * Mathf.Clamp01(st * 35f);
                    float vibrato = Mathf.Sin(2f * Mathf.PI * 5.8f * t) * 4.2f;
                    float f0 = basePitch * pitchMults[s] + vibrato;

                    // Fundamental + formantes vocales suaves filtrados estilo radio
                    float vFund = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.48f;
                    float vForm1 = Mathf.Sin(2f * Mathf.PI * (f0 * 2.0f) * t) * 0.28f;
                    float vForm2 = Mathf.Sin(2f * Mathf.PI * (f0 * 3.1f) * t) * 0.14f;
                    sample += (vFund + vForm1 + vForm2) * syllEnv * 0.42f;
                }
            }

            // 4. Clic corto de cierre de transmisión de radio (1.58s - 1.66s)
            if (t >= 1.58f && t <= 1.66f)
            {
                float tc = t - 1.58f;
                sample += Mathf.Sin(2f * Mathf.PI * 640f * tc) * Mathf.Exp(-tc * 65f) * 0.22f;
            }

            return Mathf.Clamp(sample, -0.85f, 0.85f);
        });

        return cachedVegaTransmissions[idx];
    }

    /// <summary>
    /// Sílaba / blip dinámico de la voz matemática de Vega para sincronizar letra por letra con subtítulos.
    /// Reutiliza exactamente la misma síntesis vocal cálida (fundamental + formantes 2.0x y 3.1x + vibrato 5.8 Hz + leve estática de radio)
    /// que CreateVegaVoiceTransmission, recortada a una sílaba corta sin silencios muertos al final.
    /// </summary>
    public static AudioClip CreateVegaDynamicSyllable(
        float basePitchHz = 248f,
        float pitchMultiplier = 1f,
        float durationSeconds = 0.082f,
        int seed = 0)
    {
        float dur = Mathf.Clamp(durationSeconds, 0.03f, 0.30f);
        float pitch = Mathf.Clamp(basePitchHz * pitchMultiplier, 90f, 700f);
        var rnd = new System.Random(204 + seed * 31);

        return GenerateClip($"CORE_VegaSyllable_{seed}", dur, (t, i) =>
        {
            float norm = t / dur;
            // Misma envolvente suave sin clics y ataque rápido de CreateVegaVoiceTransmission
            float syllEnv = Mathf.Sin(Mathf.PI * norm) * Mathf.Clamp01(t * 55f);
            float vibrato = Mathf.Sin(2f * Mathf.PI * 5.8f * t) * 4.2f;
            float f0 = pitch + vibrato;

            // Mismos formantes vocales cálidos filtrados estilo intercomunicador (1x, 2.0x, 3.1x)
            float vFund = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.48f;
            float vForm1 = Mathf.Sin(2f * Mathf.PI * (f0 * 2.0f) * t) * 0.28f;
            float vForm2 = Mathf.Sin(2f * Mathf.PI * (f0 * 3.1f) * t) * 0.14f;
            float radioNoise = ((float)rnd.NextDouble() * 2f - 1f) * 0.032f;

            float sample = (vFund + vForm1 + vForm2 + radioNoise) * syllEnv * 0.56f;
            return Mathf.Clamp(sample, -0.85f, 0.85f);
        });
    }

    /// <summary>Pitido doble corto de apertura del canal de Vega (misma firma de radio de CreateVegaVoiceTransmission).</summary>
    public static AudioClip CreateVegaIntercomOpenCue() => cachedVegaIntercomOpen ??= GenerateClip("CORE_VegaComms_Open", 0.135f, (t, i) =>
    {
        if (t < 0.065f)
            return Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-t * 28f) * 0.25f;
        float tp = t - 0.065f;
        return Mathf.Sin(2f * Mathf.PI * 1174.66f * tp) * Mathf.Exp(-tp * 26f) * 0.23f;
    });

    /// <summary>Clic corto de cierre del canal de Vega al terminar su línea.</summary>
    public static AudioClip CreateVegaIntercomCloseCue() => cachedVegaIntercomClose ??= GenerateClip("CORE_VegaComms_Close", 0.075f, (t, i) =>
    {
        return Mathf.Sin(2f * Mathf.PI * 640f * t) * Mathf.Exp(-t * 65f) * 0.20f;
    });

    /// <summary>Ambiente orgánico y tranquilo del biodomo del Invernadero (en bucle de 6 segundos).</summary>
    public static AudioClip CreateGreenhouseAmbience()
    {
        if (cachedGreenhouseAmbience != null) return cachedGreenhouseAmbience;
        var rnd = new System.Random(77);
        const float dur = 6.0f;
        float prev = 0f;

        cachedGreenhouseAmbience = GenerateClip("INV_AmbienteBiodomo", dur, (t, i) =>
        {
            // Filtro paso-bajo simple sobre ruido blanco para simular brisa suave entre follaje
            float white = (float)rnd.NextDouble() * 2f - 1f;
            prev = prev * 0.93f + white * 0.07f;

            // Armónico tibio del domo con ciclo exacto para que el loop no tenga cortes
            float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * (t / dur));
            float pad1 = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.08f;
            float pad2 = Mathf.Sin(2f * Mathf.PI * 196.00f * t) * 0.05f;
            return (prev * 0.42f + pad1 + pad2) * lfo * 0.35f;
        });
        return cachedGreenhouseAmbience;
    }

    /// <summary>Crujido seco de hojas marchitándose y siseo de esporas al activarse la alerta.</summary>
    public static AudioClip CreateSporeWitherSound()
    {
        if (cachedSporeWither != null) return cachedSporeWither;
        var rnd = new System.Random(313);
        cachedSporeWither = GenerateClip("INV_MarchitamientoHojas", 3.4f, (t, i) =>
        {
            float env = Mathf.Min(t * 2.5f, 1f) * Mathf.Clamp01((3.4f - t) * 0.7f);
            float hiss = ((float)rnd.NextDouble() * 2f - 1f) * 0.16f;
            // Pequeños chasquidos secos orgánicos aleatorios de hojas necrosándose
            float crackle = (rnd.NextDouble() > 0.992) ? ((float)rnd.NextDouble() * 2f - 1f) * 0.65f : 0f;
            float lowDrop = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(145f, 58f, t / 3.4f) * t) * 0.18f;
            return Mathf.Clamp((hiss + crackle + lowDrop) * env * 0.65f, -0.85f, 0.85f);
        });
        return cachedSporeWither;
    }

    /// <summary>Roce de hojas de papel al leer/recoger la Planilla de Cultivo N8 en la mesa.</summary>
    public static AudioClip CreatePaperRustleSound()
    {
        if (cachedPaperRustle != null) return cachedPaperRustle;
        var rnd = new System.Random(19);
        cachedPaperRustle = GenerateClip("INV_PapelPlanillaN8", 0.42f, (t, i) =>
        {
            float env1 = (t < 0.18f) ? Mathf.Sin(Mathf.PI * (t / 0.18f)) : 0f;
            float env2 = (t >= 0.16f) ? Mathf.Sin(Mathf.PI * ((t - 0.16f) / 0.26f)) * 0.75f : 0f;
            float noise = ((float)rnd.NextDouble() * 2f - 1f);
            return noise * (env1 + env2) * 0.28f;
        });
        return cachedPaperRustle;
    }

    /// <summary>Resonancia melancólica tenue al inspeccionar la maceta quemada con los restos de Ella.</summary>
    public static AudioClip CreateAshInspectSound() => cachedAshInspect ??= GenerateClip("INV_RestosCeniza", 1.15f, (t, i) =>
    {
        float env = Mathf.Min(t * 14f, 1f) * Mathf.Exp(-t * 2.8f);
        float chime1 = Mathf.Sin(2f * Mathf.PI * 311.13f * t) * 0.45f; // Eb4 melancólico
        float chime2 = Mathf.Sin(2f * Mathf.PI * 466.16f * t) * 0.25f; // Bb4
        return (chime1 + chime2) * env * 0.35f;
    });

    /// <summary>Brisa suave contra el vidrio al mirar por el ventanal del Invernadero.</summary>
    public static AudioClip CreateWindowWindSound()
    {
        if (cachedWindowWind != null) return cachedWindowWind;
        var rnd = new System.Random(91);
        float lp = 0f;
        cachedWindowWind = GenerateClip("INV_VentanalBrisa", 1.1f, (t, i) =>
        {
            float env = Mathf.Sin(Mathf.PI * (t / 1.1f));
            lp = lp * 0.90f + ((float)rnd.NextDouble() * 2f - 1f) * 0.10f;
            float glass = Mathf.Sin(2f * Mathf.PI * 392f * t) * 0.05f;
            return (lp * 0.55f + glass) * env * 0.45f;
        });
        return cachedWindowWind;
    }

    /// <summary>Acople neumático y cierre del domo al colocar a Ella en el carrito portátil.</summary>
    public static AudioClip CreateCartDeploySound()
    {
        if (cachedCartDeploy != null) return cachedCartDeploy;
        var rnd = new System.Random(64);
        cachedCartDeploy = GenerateClip("INV_AcopleCarrito", 0.75f, (t, i) =>
        {
            float latch = Mathf.Sin(2f * Mathf.PI * 165f * t) * Mathf.Exp(-t * 14f) * 0.55f;
            float seal = (t >= 0.12f)
                ? (((float)rnd.NextDouble() * 2f - 1f) * 0.22f + Mathf.Sin(2f * Mathf.PI * 520f * (t - 0.12f)) * 0.25f)
                  * Mathf.Exp(-(t - 0.12f) * 7.5f)
                : 0f;
            return (latch + seal) * 0.55f;
        });
        return cachedCartDeploy;
    }

    /// <summary>Apertura neumática de puerta espacial al salir hacia el Pasillo.</summary>
    public static AudioClip CreateDoorAirlockSound()
    {
        if (cachedDoorAirlock != null) return cachedDoorAirlock;
        var rnd = new System.Random(121);
        cachedDoorAirlock = GenerateClip("INV_PuertaPasillo", 0.95f, (t, i) =>
        {
            float env = Mathf.Min(t * 16f, 1f) * Mathf.Clamp01((0.95f - t) * 2.8f);
            float pneumatic = ((float)rnd.NextDouble() * 2f - 1f) * 0.22f * Mathf.Exp(-t * 3.2f);
            float slide = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(95f, 155f, t) * t) * 0.30f;
            return (pneumatic + slide) * env * 0.52f;
        });
        return cachedDoorAirlock;
    }

    private static AudioClip GenerateClip(string name, float duration, System.Func<float, int, float> generator)
    {
        int sampleCount = Mathf.RoundToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / SampleRate;
            samples[i] = generator(t, i);
        }
        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
