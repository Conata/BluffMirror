#!/usr/bin/env python3
"""
ElevenLabs API を使用してゲーム用のオーディオを生成
- AI音声（トランプ風ボイスライン）日英両対応
- 効果音（カード音、環境音など）
"""

import os
import requests
from pathlib import Path
from dotenv import load_dotenv
import time

# .env ファイルから環境変数を読み込む
load_dotenv()

ELEVEN_API_KEY = os.getenv("ELEVEN_API_KEY")
if not ELEVEN_API_KEY:
    raise ValueError("ELEVEN_API_KEY が .env ファイルに設定されていません")

# ElevenLabs API エンドポイント
TTS_API_URL = "https://api.elevenlabs.io/v1/text-to-speech"
SFX_API_URL = "https://api.elevenlabs.io/v1/sound-generation"

# 出力先ディレクトリ
VOICE_OUTPUT_DIR = Path("Baba/Assets/Audio/Voice")
SFX_OUTPUT_DIR = Path("Baba/Assets/Audio/SFX")
VOICE_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
SFX_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

# デフォルトボイスID（トランプ風の声）
# ※ 以下はRachel（女性）のID。トランプ風の男性ボイスに変更してください
# 利用可能なボイス: https://api.elevenlabs.io/v1/voices
DEFAULT_VOICE_ID = "21m00Tcm4TlvDq8ikWAM"  # Rachel


def generate_voice(text: str, filename: str, voice_id: str = DEFAULT_VOICE_ID, language: str = "en"):
    """
    ElevenLabs APIを使用して音声を生成

    Args:
        text: 音声にしたいテキスト
        filename: 保存するファイル名（拡張子なし）
        voice_id: 使用するボイスのID
        language: 言語 ("en" または "ja")
    """
    url = f"{TTS_API_URL}/{voice_id}"

    headers = {
        "Accept": "audio/mpeg",
        "Content-Type": "application/json",
        "xi-api-key": ELEVEN_API_KEY
    }

    # 言語に応じてモデルを選択
    model_id = "eleven_multilingual_v2" if language == "ja" else "eleven_turbo_v2_5"

    data = {
        "text": text,
        "model_id": model_id,
        "voice_settings": {
            "stability": 0.5,
            "similarity_boost": 0.75,
            "style": 0.5,
            "use_speaker_boost": True
        }
    }

    print(f"生成中: {filename}.mp3 ({language}) - \"{text[:50]}...\"")

    response = requests.post(url, json=data, headers=headers)

    if response.status_code == 200:
        output_path = VOICE_OUTPUT_DIR / f"{filename}.mp3"
        with open(output_path, "wb") as f:
            f.write(response.content)
        print(f"  ✓ 保存完了: {output_path}")
        return True
    else:
        print(f"  ✗ エラー: {response.status_code} - {response.text}")
        return False


def generate_sound_effect(prompt: str, filename: str, duration: float = 1.0):
    """
    ElevenLabs Sound Effects APIを使用して効果音を生成

    Args:
        prompt: 効果音の説明（英語）
        filename: 保存するファイル名（拡張子なし）
        duration: 効果音の長さ（秒）
    """
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
    """メイン関数 - すべてのオーディオを生成"""

    print("=" * 60)
    print("ElevenLabs オーディオ生成スクリプト")
    print("=" * 60)
    print()

    # ============================================
    # Part 1: AI音声（ボイスライン）
    # ============================================
    print("[ Part 1: AI音声生成 ]")
    print("=" * 60)

    # 英語ボイスライン
    english_lines = {
        # ゲーム開始
        "game_start_1": "Let's play. I'm the best at this game, believe me.",
        "game_start_2": "You ready? This is going to be huge!",

        # カードドロー
        "card_draw_1": "Oh, this is a good one!",
        "card_draw_2": "Tremendous card!",
        "card_draw_3": "Nobody draws better cards than me.",

        # ペア成立
        "pair_match_1": "Perfect match! Just like I planned.",
        "pair_match_2": "See? I told you. The best.",

        # 勝利
        "victory_1": "I won! Of course I won. I always win!",
        "victory_2": "Too easy. Nobody beats me at cards.",

        # 敗北
        "defeat_1": "This game is rigged! Totally unfair!",
        "defeat_2": "I demand a recount!",

        # 心理圧
        "pressure_1": "You're sweating. I can tell.",
        "pressure_2": "Feeling the pressure? You should be.",
        "pressure_3": "This is what winning looks like.",
    }

    # 日本語ボイスライン
    japanese_lines = {
        # ゲーム開始
        "game_start_1_ja": "始めよう。私はこのゲームで最高だ、信じてくれ。",
        "game_start_2_ja": "準備はいいか？これは凄いことになるぞ！",

        # カードドロー
        "card_draw_1_ja": "おお、これは良いカードだ！",
        "card_draw_2_ja": "素晴らしいカードだ！",
        "card_draw_3_ja": "私ほど良いカードを引く者はいない。",

        # ペア成立
        "pair_match_1_ja": "完璧なマッチだ！計画通りだ。",
        "pair_match_2_ja": "ほら、言っただろう。最高だ。",

        # 勝利
        "victory_1_ja": "勝った！当然だ。私はいつも勝つ！",
        "victory_2_ja": "簡単すぎる。カードで私に勝てる者はいない。",

        # 敗北
        "defeat_1_ja": "このゲームは不正だ！完全に不公平だ！",
        "defeat_2_ja": "再集計を要求する！",

        # 心理圧
        "pressure_1_ja": "汗をかいているな。見てわかるぞ。",
        "pressure_2_ja": "プレッシャーを感じているか？感じるべきだ。",
        "pressure_3_ja": "これが勝利というものだ。",
    }

    print("\n[ 英語ボイスライン ]")
    print("-" * 60)
    for filename, text in english_lines.items():
        generate_voice(text, filename, language="en")
        time.sleep(0.5)  # API制限を考慮

    print("\n[ 日本語ボイスライン ]")
    print("-" * 60)
    for filename, text in japanese_lines.items():
        generate_voice(text, filename, language="ja")
        time.sleep(0.5)

    # ============================================
    # Part 2: 効果音
    # ============================================
    print("\n\n[ Part 2: 効果音生成 ]")
    print("=" * 60)

    sound_effects = {
        # カード音（最小0.5秒）
        "card_hover": ("Soft subtle card rustling sound, gentle paper movement", 0.5),
        "card_pick": ("Quick card pick up sound, crisp paper grab", 0.5),
        "card_place": ("Card placing on felt table, soft thud on fabric", 0.5),
        "card_flip_1": ("Playing card flip sound, quick snap", 0.5),
        "card_flip_2": ("Playing card flip sound, smooth turn", 0.5),
        "card_flip_3": ("Playing card flip sound, rapid flip", 0.5),

        # 環境音
        "room_ambience": ("Quiet indoor room ambience, subtle air, calm atmosphere", 5.0),
        "felt_slide": ("Card sliding on felt fabric surface, smooth friction", 0.8),

        # 心理効果音
        "whisper_ambience": ("Creepy subtle whispers in the background, unsettling atmosphere", 3.0),
    }

    print("\n[ 効果音 ]")
    print("-" * 60)
    for filename, (prompt, duration) in sound_effects.items():
        generate_sound_effect(prompt, filename, duration)
        time.sleep(1.0)  # 効果音生成は時間がかかるため長めに待機

    print()
    print("=" * 60)
    print("✓ すべてのオーディオ生成が完了しました")
    print(f"✓ AI音声保存先: {VOICE_OUTPUT_DIR.absolute()}")
    print(f"✓ 効果音保存先: {SFX_OUTPUT_DIR.absolute()}")
    print("=" * 60)
    print()
    print("【次のステップ】")
    print("1. Unity Editorで生成された音声ファイルをインポート")
    print("2. AudioManagerの各フィールドに音声ファイルを割り当て")
    print("3. Context Menuでテスト実行")
    print("=" * 60)


if __name__ == "__main__":
    main()
