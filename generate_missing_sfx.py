#!/usr/bin/env python3
"""
失敗した効果音のみを再生成
"""

import os
import requests
from pathlib import Path
from dotenv import load_dotenv
import time

load_dotenv()

ELEVEN_API_KEY = os.getenv("ELEVEN_API_KEY")
SFX_API_URL = "https://api.elevenlabs.io/v1/sound-generation"
SFX_OUTPUT_DIR = Path("Baba/Assets/Audio/SFX")
SFX_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def generate_sound_effect(prompt: str, filename: str, duration: float = 0.5):
    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": ELEVEN_API_KEY
    }

    data = {
        "text": prompt,
        "duration_seconds": duration,
        "prompt_influence": 0.3
    }

    print(f"生成中: {filename}.mp3 - \"{prompt}\"")

    response = requests.post(SFX_API_URL, json=data, headers=headers)

    if response.status_code == 200:
        output_path = SFX_OUTPUT_DIR / f"{filename}.mp3"
        with open(output_path, "wb") as f:
            f.write(response.content)
        print(f"  ✓ 保存完了: {output_path}")
        return True
    else:
        print(f"  ✗ エラー: {response.status_code} - {response.text}")
        return False


def main():
    print("=" * 60)
    print("失敗した効果音の再生成")
    print("=" * 60)
    print()

    missing_effects = {
        "card_hover": ("Soft subtle card rustling sound, gentle paper movement", 0.5),
        "card_pick": ("Quick card pick up sound, crisp paper grab", 0.5),
        "card_flip_1": ("Playing card flip sound, quick snap", 0.5),
        "card_flip_2": ("Playing card flip sound, smooth turn", 0.5),
        "card_flip_3": ("Playing card flip sound, rapid flip", 0.5),
    }

    for filename, (prompt, duration) in missing_effects.items():
        generate_sound_effect(prompt, filename, duration)
        time.sleep(1.0)

    print()
    print("=" * 60)
    print("✓ 完了しました")
    print(f"✓ 保存先: {SFX_OUTPUT_DIR.absolute()}")
    print("=" * 60)


if __name__ == "__main__":
    main()
