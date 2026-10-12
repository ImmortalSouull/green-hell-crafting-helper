"""Vertical (1080x1920) TikTok cut of the Crafting Helper showcase.

The recipe book window sits in the middle of the 3440x1440 take; it's cropped close so the text
stays readable on a phone. Big two-line captions sit above the footage.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = [os.path.join(HERE, "raw", "take3.mp4"), os.path.join(HERE, "raw", "take4.mp4")]
OUT = os.path.join(HERE, sys.argv[1] if len(sys.argv) > 1 else "gh_crafting_helper_tiktok.mp4")
FONT = "C\\:/Windows/Fonts/segoeuib.ttf"
FONT_REG = "C\\:/Windows/Fonts/segoeui.ttf"

W, H = 1080, 1920
VIDEO_Y = 420
ACCENT = "0xE8C25A"

# (source, in, dur, crop_x, crop_w, line1, line2); crop_w is scaled to 1080 wide
SEGMENTS = [
    (0, 5.6, 3.0, 1070, 1100, "Green Hell", "no more wiki on a second screen"),
    (0, 8.6, 4.2, 1070, 1100, "Search any item", "or ingredient"),
    (0, 13.0, 4.0, 1660, 900, "See what you can", "craft right now"),
    (0, 17.2, 3.4, 1660, 900, "One click:", "ingredients on the table"),
    (0, 20.6, 4.6, 1720, 1080, "Press Craft", "done"),
    (1, 3.2, 3.2, 1070, 1100, "Pin a recipe", ""),
    (1, 7.5, 7.5, 560, 1080, "Live counter", "while you gather"),
]
CARD_DUR = 3.4
FADE = 0.15


def esc(s):
    return s.replace("\\", "\\\\").replace(":", "\\:").replace("'", "’").replace("%", "\\%")


def text(label, y, size, color="white", font=FONT):
    return (f"drawtext=fontfile='{font}':text='{esc(label)}':fontsize={size}:fontcolor={color}"
            f":x=(w-text_w)/2:y={y}:shadowcolor=black@0.7:shadowx=3:shadowy=3")


parts, labels = [], []
for i, (src, t_in, dur, cx, cw, l1, l2) in enumerate(SEGMENTS):
    ch = min(1440, int(cw * 1350 / 1080))
    cy = (1440 - ch) // 2
    v = (f"[{src}:v]trim=start={t_in}:duration={dur},setpts=PTS-STARTPTS,"
         f"crop={cw}:{ch}:{cx}:{cy},scale=1080:-2,"
         f"pad={W}:{H}:0:{VIDEO_Y}:color=0x0B0D0A,"
         + text(l1, 120 if l2 else 170, 80, ACCENT if i == 0 else "white") + ","
         + (text(l2, 225, 60) + "," if l2 else "")
         + text("Crafting Helper", 1800, 44, ACCENT) + ","
         f"fade=t=in:st=0:d={FADE},fade=t=out:st={dur - FADE}:d={FADE},format=yuv420p,setsar=1[v{i}]")
    a = (f"[{src}:a]atrim=start={t_in}:duration={dur},asetpts=PTS-STARTPTS,"
         f"afade=t=in:st=0:d={FADE},afade=t=out:st={dur - FADE}:d={FADE}[a{i}]")
    parts += [v, a]
    labels.append(f"[v{i}][a{i}]")

n = len(SEGMENTS)
card = (f"color=c=0x0B0D0A:s={W}x{H}:d={CARD_DUR}:r=30,"
        + text("Crafting Helper", 720, 110, ACCENT) + ","
        + text("free mod for Green Hell", 860, 56) + ","
        + text("press K in game", 960, 48, "white", FONT_REG) + ","
        + f"fade=t=in:st=0:d=0.3,fade=t=out:st={CARD_DUR - 0.8}:d=0.8,format=yuv420p,setsar=1[v{n}]")
parts.append(card)
parts.append(f"anullsrc=r=48000:cl=stereo,atrim=duration={CARD_DUR}[a{n}]")
labels.append(f"[v{n}][a{n}]")
parts.append("".join(labels) + f"concat=n={n + 1}:v=1:a=1[vc][ac]")
parts.append("[ac]loudnorm=I=-14:TP=-1.5:LRA=11[ao]")

cmd = ["ffmpeg", "-v", "error", "-y"]
for s in SRC:
    cmd += ["-i", s]
cmd += ["-filter_complex", ";".join(parts), "-map", "[vc]", "-map", "[ao]", "-r", "30",
        "-c:v", "libx264", "-preset", "medium", "-crf", "21", "-pix_fmt", "yuv420p",
        "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-movflags", "+faststart", OUT]
subprocess.run(cmd, check=True)
print("wrote", OUT)
