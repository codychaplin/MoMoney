#!/usr/bin/env python3
"""
Takes colours from light/dark objects, halves their saturation,
and writes desaturated versions into light/dark; originals go into lightMaterialYou/darkMaterialYou.
"""

import json
import colorsys
import sys
from pathlib import Path


def hex_to_rgb(hex_color: str) -> tuple[int, int, int]:
    h = hex_color.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def rgb_to_hex(r: int, g: int, b: int) -> str:
    return f"#{r:02X}{g:02X}{b:02X}"


def half_saturation(hex_color: str) -> str:
    r, g, b = hex_to_rgb(hex_color)
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    s /= 6
    r2, g2, b2 = colorsys.hsv_to_rgb(h, s, v)
    return rgb_to_hex(round(r2 * 255), round(g2 * 255), round(b2 * 255))


SKIP_KEYS = ("primary", "secondary", "tertiary")


def process_palette(palette: dict) -> dict:
    result = {}
    for key, value in palette.items():
        if any(word in key.lower() for word in SKIP_KEYS):
            result[key] = value
        else:
            result[key] = half_saturation(value)
    return result


def main(path: str) -> None:
    file = Path(path)
    data = json.loads(file.read_text())

    data["light"] = process_palette(data["lightMaterialYou"])
    data["dark"] = process_palette(data["darkMaterialYou"])

    file.write_text(json.dumps(data, indent=4))
    print(f"Updated {file}")


if __name__ == "__main__":
    if len(sys.argv) > 1:
        main(sys.argv[1])
    else:
        for f in sorted(Path("MoMoney/Resources/Raw").glob("theme*.json")):
            main(str(f))
