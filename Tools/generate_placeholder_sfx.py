"""Generates the placeholder SFX stand-ins (mono, 16-bit, 22.05 kHz WAV) into Assets/_Project/Audio/Placeholder.

Standard library only. Re-run any time; output is deterministic (fixed seeds). The file name (minus a trailing _1 / _2
variation suffix) is the SfxId enum name, which is how the SfxLibrary builder (Tools > Audio > Rebuild SfxLibrary in the
Unity Editor) maps clips to ids. Replace any WAV with real audio of the same name and rebuild the library.

Usage: python Tools/generate_placeholder_sfx.py
"""
import math
import os
import random
import struct
import wave

RATE = 22050
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets', '_Project', 'Audio', 'Placeholder')


def n(seconds):
    return int(RATE * seconds)


def env(i, total, attack=0.005, release=0.6):
    """Fast linear attack, exponential-ish decay over the rest."""
    t = i / RATE
    a = min(1.0, t / attack) if attack > 0 else 1.0
    r = 1.0 - (i / total)
    return a * (r ** (1.0 + release * 2.0))


def sine(freq, seconds, amp=1.0, **kw):
    total = n(seconds)
    return [amp * math.sin(2 * math.pi * freq * i / RATE) * env(i, total, **kw) for i in range(total)]


def sweep(f0, f1, seconds, amp=1.0, **kw):
    total = n(seconds)
    phase = 0.0
    out = []
    for i in range(total):
        f = f0 + (f1 - f0) * (i / total)
        phase += 2 * math.pi * f / RATE
        out.append(amp * math.sin(phase) * env(i, total, **kw))
    return out


def noise(seconds, amp=1.0, lowpass=0.2, seed=1, **kw):
    """White noise through a one-pole low-pass (lowpass 0..1, smaller = darker)."""
    rng = random.Random(seed)
    total = n(seconds)
    y = 0.0
    out = []
    for i in range(total):
        y += lowpass * (rng.uniform(-1, 1) - y)
        out.append(amp * y * env(i, total, **kw))
    return out


def mix(*tracks):
    length = max(len(t) for t in tracks)
    out = [0.0] * length
    for t in tracks:
        for i, v in enumerate(t):
            out[i] += v
    return out


def seq(*parts):
    out = []
    for p in parts:
        out.extend(p)
    return out


def silence(seconds):
    return [0.0] * n(seconds)


def gain(track, g):
    return [v * g for v in track]


def normalize(track, peak=0.8):
    m = max(abs(v) for v in track) or 1.0
    return [v / m * peak for v in track]


def write(name, track, peak=0.8):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.wav')
    data = normalize(track, peak)
    with wave.open(path, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b''.join(struct.pack('<h', int(max(-1.0, min(1.0, v)) * 32767)) for v in data))
    print('wrote', os.path.relpath(path))


def seamless_loop(track, fade=0.05):
    """Crossfades the tail into the head so the loop point has no click."""
    f = n(fade)
    body = track[:-f]
    for i in range(f):
        k = i / f
        body[i] = body[i] * k + track[len(track) - f + i] * (1 - k)
    return body


def build():
    # ---- UI ----
    write('UiClick', mix(sweep(900, 600, 0.07, release=0.4), gain(noise(0.02, lowpass=0.6, seed=2), 0.2)), 0.8)
    write('UiToggle', seq(sine(700, 0.05), sine(1050, 0.07)), 0.8)
    write('UiSliderTick', mix(sine(1300, 0.08, release=0.25), gain(sine(2600, 0.08, release=0.25), 0.35)), 0.95)
    write('CountdownTick', sine(660, 0.12, release=0.5), 0.85)
    write('CountdownGo', mix(sweep(660, 1100, 0.4), sine(1320, 0.4), gain(sine(1760, 0.4), 0.5)), 0.8)

    # ---- Crashes and hits (two variations each) ----
    for k, seed in ((1, 11), (2, 12)):
        pitch = 90 + 12 * k
        write('CrashPlain_%d' % k, mix(noise(0.35, lowpass=0.18, seed=seed, release=0.8), sweep(pitch * 2, pitch, 0.3, amp=0.9)), 0.85)
        write('Hit_%d' % k, mix(noise(0.2, lowpass=0.35, seed=seed + 10, release=0.7), sweep(180 + 20 * k, 90, 0.18, amp=0.8)), 0.8)
    write('CrashMistimed', mix(noise(0.25, lowpass=0.1, seed=31, release=0.9), sweep(110, 55, 0.3, amp=0.9)), 0.75)
    write('CrashPerfect', mix(
        mix(noise(0.4, lowpass=0.2, seed=21, release=0.8), sweep(200, 70, 0.35, amp=0.9)),
        seq(silence(0.08), sine(800, 0.25), sine(1200, 0.25), sine(1600, 0.4))), 0.9)

    # ---- Pickups / feedback ----
    write('Heal', seq(sweep(500, 700, 0.15), sweep(700, 1000, 0.25)), 0.7)
    write('Pickup', seq(sine(880, 0.09), sine(1320, 0.16)), 0.7)
    write('ComboTick', sine(1000, 0.08, release=0.4), 0.6)

    # ---- Boost ----
    write('BoostStart', mix(noise(0.45, lowpass=0.25, seed=41, attack=0.25, release=0.2), sweep(150, 420, 0.45, amp=0.5, attack=0.2)), 0.7)
    loop = mix(noise(1.2, lowpass=0.12, seed=42, attack=0.0, release=0.0), gain(sine(70, 1.2, attack=0.0, release=0.0), 0.5))
    loop = [v * 1.0 for v in loop]
    write('BoostLoop', seamless_loop(loop), 0.45)

    # ---- Warning beeps (cadence is code-driven) ----
    write('WarnCeiling', sine(880, 0.14, release=0.3), 0.7)
    write('WarnLateral', seq(sine(600, 0.08), silence(0.03), sine(600, 0.08)), 0.7)
    write('WarnGround', seq(sweep(520, 380, 0.14), sweep(520, 380, 0.14)), 0.8)
    write('WarnLowFuel', seq(sine(520, 0.07), silence(0.04), sine(520, 0.07)), 0.7)
    write('WarnLowHealth', seq(sweep(160, 90, 0.1, amp=1.0), silence(0.06), sweep(150, 80, 0.12, amp=0.8)), 0.8)

    # ---- Run end ----
    write('RunEndWreck', mix(noise(0.9, lowpass=0.15, seed=51, release=1.0), sweep(120, 40, 0.8, amp=1.0)), 0.9)
    write('RunEndFuel', seq(*[sweep(300 - 40 * i, 200 - 40 * i, 0.12, amp=1.0 - 0.15 * i) + silence(0.05 + 0.03 * i) for i in range(5)]), 0.75)
    write('RunEndFlyOff', mix(noise(0.7, lowpass=0.3, seed=61, attack=0.15, release=0.8), sweep(300, 1100, 0.7, amp=0.4, attack=0.15)), 0.7)
    write('RunEndGround', mix(noise(0.5, lowpass=0.1, seed=71, release=1.0), sweep(100, 40, 0.45, amp=1.0)), 0.9)
    write('GameOver', seq(sine(523, 0.18), sine(440, 0.18), sine(370, 0.18), sweep(311, 220, 0.5)), 0.7)


if __name__ == '__main__':
    build()
