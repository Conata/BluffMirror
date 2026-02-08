#!/bin/bash
# Stage 14 Quick Test Script

echo "🎮 Stage 14 Test Checklist"
echo ""
echo "1. Play Mode でゲームを開始"
echo "2. 数ターンプレイしてリザルト画面へ"
echo ""
echo "✅ Check Console logs for:"
echo "   - [GameSessionRecorder] Session finalized"
echo "   - [ResultDiagnosisPrompt] Turn history section"
echo "   - [ResultDiagnosisPrompt] Game advantage section"
echo ""
echo "✅ Check Result UI for:"
echo "   - Chain-of-Thought style text"
echo "   - Specific numerical data (decision time, doubt level)"
echo "   - References to game situations (card advantage, Joker holding)"
echo ""
echo "🔍 Optional: Check full prompt with Debug.Log in GenerateLLMDiagnosisAsync"
