"""Erzeugt die mitgelieferte Symbolbibliothek (src/BiMaDock/Icons/<Kategorie>/<Name>.png).

Jedes Symbol ist eine abgerundete Kachel mit Farbverlauf der Kategorie und einem weißen Motiv
aus der Windows-Schrift "Segoe Fluent Icons". Aufruf: python tools/generate-icons.py
Benötigt Pillow und Windows 11 (Schrift SegoeIcons.ttf).
"""
from functools import lru_cache
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SIZE = 256
SS = 2  # Supersampling für glatte Kanten
FONT = r"C:\Windows\Fonts\SegoeIcons.ttf"
OUT = Path(__file__).resolve().parent.parent / "src" / "BiMaDock" / "Icons"

# Kategorie: (Verlauf oben links, Verlauf unten rechts, {Name: Codepoint})
LIBRARY = {
    "Allgemein": ("#4FACFE", "#1E5BD8", {
        "Start": 0xE80F, "Web": 0xE774, "Mail": 0xE715, "Dokument": 0xE8A5, "Ordner": 0xE8B7,
        "Ordner offen": 0xE838, "Bilder": 0xE8B9, "Downloads": 0xE896, "Kalender": 0xE787,
        "Suche": 0xE721, "Notizen": 0xE70F, "Favorit": 0xE734, "Herz": 0xEB51, "Glocke": 0xEA8F,
        "Link": 0xE71B, "Uhr": 0xE823, "Chat": 0xE8F2,
    }),
    "Büro": ("#43C6AC", "#19746B", {
        "Rechner": 0xE8EF, "Drucker": 0xE749, "Bibliothek": 0xE8F1, "Kontakte": 0xE716,
        "Visitenkarte": 0xE8B8, "Liste": 0xE8FD, "Diagramm": 0xE9D2, "Bericht": 0xE9F9,
        "Lernen": 0xE7BE, "Buch": 0xE82D, "Speichern": 0xE74E, "Etikett": 0xE8EC,
    }),
    "Entwicklung": ("#A18CD1", "#5B3FB5", {
        "Code": 0xE943, "Konsole": 0xE756, "Fehlersuche": 0xEBE8, "Paket": 0xE7B8,
        "Erweiterung": 0xEA86, "Zahnräder": 0xE9F5, "Werkzeug": 0xEC7A, "Entwurf": 0xEB3C,
        "Schraubenschlüssel": 0xE90F, "Chip": 0xE950,
    }),
    "Netzwerk und Server": ("#2BC0E4", "#13607C", {
        "Netzwerk": 0xE968, "WLAN": 0xE701, "Ethernet": 0xE839, "Funkmast": 0xEC05,
        "Cloud": 0xE753, "Synchronisieren": 0xE895, "Server": 0xEDA2, "Knoten": 0xF22C,
        "Firewall": 0xE83D, "Schloss": 0xE72E, "Schlüssel": 0xE8D7, "Fingerabdruck": 0xE928,
        "Monitor": 0xE7F4, "Laptop": 0xE7F8, "Hochladen": 0xE898, "Fernzugriff": 0xE703,
    }),
    "Smart Home": ("#FFB347", "#E8590C", {
        "Licht": 0xE82F, "Temperatur": 0xE9CA, "Sonne": 0xE706, "Nacht": 0xE708,
        "Strom": 0xE945, "Ein Aus": 0xE7E8, "Verbrauch": 0xEC4A, "Regler": 0xE9E9,
        "Haus": 0xE80F, "Diagnose": 0xE9D9, "Akku": 0xE83E,
    }),
    "Medien": ("#FF6B8B", "#C2185B", {
        "Musik": 0xE8D6, "Video": 0xE714, "Kamera": 0xE722, "Kopfhörer": 0xE7F6,
        "Mikrofon": 0xE720, "Wiedergabe": 0xE768, "Aufnahme": 0xE7C8, "Pinsel": 0xE771,
        "Farben": 0xE790, "Galerie": 0xE91B,
    }),
    "Freizeit und 3D-Druck": ("#7ED957", "#2E8B3A", {
        "Spiele": 0xE7FC, "3D-Modell": 0xF158, "Druckschichten": 0xE81E, "Karte": 0xE707,
        "Auto": 0xE804, "Bus": 0xE806, "Flagge": 0xE7C1, "Feuerwehr": 0xECAD,
        "KI": 0xE99A, "Ideen": 0xE794, "Daumen hoch": 0xE8E1, "Smiley": 0xED54,
    }),
    "System": ("#8E9EAB", "#4A5560", {
        "Einstellungen": 0xE713, "Info": 0xE946, "Warnung": 0xE7BA, "Papierkorb": 0xE74D,
        "Verlauf": 0xE81C, "Apps": 0xF0E2, "Raster": 0xE80A, "Neu laden": 0xE72C,
        "Teilen": 0xE72D, "Hilfe": 0xE897,
    }),
}


def hex_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4))


@lru_cache
def diagonal(c1: tuple[int, int, int], c2: tuple[int, int, int]) -> Image.Image:
    """Verlauf von oben links nach unten rechts."""
    s = SIZE * SS
    line = Image.new("RGBA", (2 * s, 1))
    for x in range(2 * s):
        t = x / (2 * s)
        line.putpixel((x, 0), tuple(round(c1[i] + (c2[i] - c1[i]) * t) for i in range(3)) + (255,))
    big = line.resize((2 * s, 2 * s)).rotate(45, resample=Image.BICUBIC)
    return big.crop((s // 2, s // 2, s // 2 + s, s // 2 + s))


def tile(top: str, bottom: str, codepoint: int) -> Image.Image:
    s = SIZE * SS
    margin = 14 * SS
    radius = 58 * SS
    c1, c2 = hex_rgb(top), hex_rgb(bottom)

    gradient = diagonal(c1, c2)

    mask = Image.new("L", (s, s), 0)
    ImageDraw.Draw(mask).rounded_rectangle((margin, margin, s - margin, s - margin), radius, fill=255)

    result = Image.new("RGBA", (s, s), (0, 0, 0, 0))

    # weicher Schatten unter der Kachel
    shadow = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle((margin, margin + 6 * SS, s - margin, s - margin + 6 * SS), radius, fill=(0, 0, 0, 90))
    result.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(6 * SS)))

    body = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    body.paste(gradient, (0, 0), mask)

    # Glanz in der oberen Hälfte
    gloss = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    gd = ImageDraw.Draw(gloss)
    for y in range(margin, s // 2):
        alpha = round(46 * (1 - (y - margin) / (s // 2 - margin)))
        gd.line((0, y, s, y), fill=(255, 255, 255, alpha))
    gloss_mask = Image.new("L", (s, s), 0)
    gloss_mask.paste(mask, (0, 0))
    body.alpha_composite(Image.composite(gloss, Image.new("RGBA", (s, s)), gloss_mask))

    # feiner heller Innenrand
    ImageDraw.Draw(body).rounded_rectangle((margin, margin, s - margin, s - margin), radius,
                                           outline=(255, 255, 255, 60), width=2 * SS)
    result.alpha_composite(body)

    # Motiv mit leichtem Schatten
    font = ImageFont.truetype(FONT, 124 * SS)
    glyph = chr(codepoint)
    box = font.getbbox(glyph)
    gx = (s - (box[2] - box[0])) / 2 - box[0]
    gy = (s - (box[3] - box[1])) / 2 - box[1]
    glyph_shadow = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    ImageDraw.Draw(glyph_shadow).text((gx, gy + 4 * SS), glyph, font=font, fill=(0, 0, 0, 80))
    result.alpha_composite(glyph_shadow.filter(ImageFilter.GaussianBlur(3 * SS)))
    ImageDraw.Draw(result).text((gx, gy), glyph, font=font, fill=(255, 255, 255, 255))

    return result.resize((SIZE, SIZE), Image.LANCZOS)


def main() -> None:
    count = 0
    for category, (top, bottom, icons) in LIBRARY.items():
        folder = OUT / category
        folder.mkdir(parents=True, exist_ok=True)
        for name, codepoint in icons.items():
            tile(top, bottom, codepoint).save(folder / f"{name}.png", optimize=True)
            count += 1
    print(f"{count} Symbole in {OUT}")


if __name__ == "__main__":
    main()
