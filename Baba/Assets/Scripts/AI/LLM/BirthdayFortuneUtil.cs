using System;
using System.Linq;
using UnityEngine;

namespace FPSTrump.AI.LLM
{
    /// <summary>
    /// 四柱推命・数秘術ベースの行動パターン分析ユーティリティ
    /// 生年月日から五行分析・数秘術ナンバーを算出し、表示用テキスト断片を生成
    /// </summary>
    public static class BirthdayFortuneUtil
    {
        // ===== 五行 (Five Elements) =====

        public enum ElementType
        {
            Wood = 0,   // 木 - 成長・柔軟性
            Fire = 1,   // 火 - 情熱・行動力
            Earth = 2,  // 土 - 安定・慎重さ
            Metal = 3,  // 金 - 集中・完璧主義
            Water = 4   // 水 - 適応・直感
        }

        /// <summary>
        /// 四柱推命: 年柱を算出（性格の基盤）
        /// </summary>
        public static ElementType CalculateYearPillar(int year)
        {
            int lastDigit = year % 10;
            return lastDigit switch
            {
                0 or 1 => ElementType.Metal,
                2 or 3 => ElementType.Water,
                4 or 5 => ElementType.Wood,
                6 or 7 => ElementType.Fire,
                8 or 9 => ElementType.Earth,
                _ => ElementType.Earth
            };
        }

        /// <summary>
        /// 四柱推命: 月柱を算出（対人関係・感情傾向）
        /// </summary>
        public static ElementType CalculateMonthPillar(int month)
        {
            return month switch
            {
                1 or 2 or 12 => ElementType.Water,  // 冬
                3 or 4 or 5 => ElementType.Wood,     // 春
                6 or 7 or 8 => ElementType.Fire,     // 夏
                9 or 10 or 11 => ElementType.Metal,  // 秋
                _ => ElementType.Earth
            };
        }

        /// <summary>
        /// 四柱推命: 日柱を算出（核となる性格）
        /// </summary>
        public static ElementType CalculateDayPillar(int day)
        {
            return (ElementType)(day % 5);
        }

        /// <summary>
        /// 数秘術: ライフパスナンバー算出
        /// マスターナンバー（11, 22, 33）は保持
        /// </summary>
        public static int CalculateLifePathNumber(int year, int month, int day)
        {
            int yearSum = SumDigits(year);
            int totalSum = yearSum + month + day;
            return ReduceToSingleDigit(totalSum);
        }

        /// <summary>
        /// 数秘術: パーソナリティナンバー算出（月日から）
        /// </summary>
        public static int CalculatePersonalityNumber(int month, int day)
        {
            return ReduceToSingleDigit(month + day);
        }

        /// <summary>
        /// 数秘術: ソウルナンバー算出（年から）
        /// </summary>
        public static int CalculateSoulNumber(int year)
        {
            return ReduceToSingleDigit(SumDigits(year));
        }

        // ===== 表示用テキスト生成 =====

        /// <summary>
        /// 五行の名前を取得（ローカライズ対応）
        /// </summary>
        public static string GetElementName(ElementType element)
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string key = element switch
                {
                    ElementType.Wood => "fortune.element_wood",
                    ElementType.Fire => "fortune.element_fire",
                    ElementType.Earth => "fortune.element_earth",
                    ElementType.Metal => "fortune.element_metal",
                    ElementType.Water => "fortune.element_water",
                    _ => null
                };
                if (key != null) return loc.Get(key);
            }
            return element.ToString();
        }

        /// <summary>
        /// 五行の特性キーワード（ローカライズ対応）
        /// </summary>
        public static string GetElementTrait(ElementType element)
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string key = element switch
                {
                    ElementType.Wood => "fortune.trait_wood",
                    ElementType.Fire => "fortune.trait_fire",
                    ElementType.Earth => "fortune.trait_earth",
                    ElementType.Metal => "fortune.trait_metal",
                    ElementType.Water => "fortune.trait_water",
                    _ => null
                };
                if (key != null) return loc.Get(key);
            }
            return "";
        }

        /// <summary>
        /// ライフパスナンバーの意味（ローカライズ対応）
        /// </summary>
        public static string GetLifePathMeaning(int number)
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string key = $"fortune.life_path_{number}";
                string result = loc.Get(key);
                if (result != key) return result;
                return loc.Get("fortune.life_path_default");
            }
            return number.ToString();
        }

        /// <summary>
        /// イントロ用の占い断片テキストを生成（3-4断片）
        /// AIが分析結果を小出しに見せる演出用
        /// </summary>
        public static string[] GetFortuneFragments(int year, int month, int day)
        {
            var yearPillar = CalculateYearPillar(year);
            var monthPillar = CalculateMonthPillar(month);
            var dayPillar = CalculateDayPillar(day);
            int lifePathNumber = CalculateLifePathNumber(year, month, day);
            int personalityNumber = CalculatePersonalityNumber(month, day);

            string yearName = GetElementName(yearPillar);
            string monthName = GetElementName(monthPillar);
            string dayName = GetElementName(dayPillar);
            string dayTrait = GetElementTrait(dayPillar);
            string lifePathMeaning = GetLifePathMeaning(lifePathNumber);

            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                return new string[]
                {
                    LocalizationManager.ApplyVars(loc.Get("fortune.fragment_pillar"), ("yearName", yearName), ("monthName", monthName), ("dayName", dayName)),
                    LocalizationManager.ApplyVars(loc.Get("fortune.fragment_day"), ("dayName", dayName), ("dayTrait", dayTrait)),
                    LocalizationManager.ApplyVars(loc.Get("fortune.fragment_life_path"), ("lifePathNumber", lifePathNumber.ToString()), ("lifePathMeaning", lifePathMeaning)),
                    GetSynthesisFragment(dayPillar, lifePathNumber)
                };
            }

            return new string[]
            {
                $"{yearName} / {monthName} / {dayName}",
                $"{dayName} - {dayTrait}",
                $"Life Path {lifePathNumber} - {lifePathMeaning}",
                GetSynthesisFragment(dayPillar, lifePathNumber)
            };
        }

        /// <summary>
        /// LLMプロンプト用の占い分析コンテキストを生成
        /// </summary>
        public static string BuildFortuneContext(int year, int month, int day)
        {
            var yearPillar = CalculateYearPillar(year);
            var monthPillar = CalculateMonthPillar(month);
            var dayPillar = CalculateDayPillar(day);
            int lifePathNumber = CalculateLifePathNumber(year, month, day);
            int personalityNumber = CalculatePersonalityNumber(month, day);
            int soulNumber = CalculateSoulNumber(year);

            string yearName = GetElementName(yearPillar);
            string monthName = GetElementName(monthPillar);
            string dayName = GetElementName(dayPillar);
            string dayTrait = GetElementTrait(dayPillar);
            string lifePathMeaning = GetLifePathMeaning(lifePathNumber);

            return $@"
- Four Pillars analysis: Year={yearName}({yearPillar}), Month={monthName}({monthPillar}), Day={dayName}({dayPillar})
- Day pillar trait: {dayTrait}
- Life Path Number: {lifePathNumber} ({lifePathMeaning})
- Personality Number: {personalityNumber}
- Soul Number: {soulNumber}";
        }

        // ===== Private Helpers =====

        private static int SumDigits(int number)
        {
            int sum = 0;
            number = Math.Abs(number);
            while (number > 0)
            {
                sum += number % 10;
                number /= 10;
            }
            return sum;
        }

        private static int ReduceToSingleDigit(int number)
        {
            while (number > 9 && number != 11 && number != 22 && number != 33)
            {
                int sum = 0;
                while (number > 0)
                {
                    sum += number % 10;
                    number /= 10;
                }
                number = sum;
            }
            return number;
        }

        /// <summary>
        /// 総合判断の匂わせ断片
        /// </summary>
        private static string GetSynthesisFragment(ElementType dayPillar, int lifePathNumber)
        {
            bool isIntuitive = dayPillar == ElementType.Water || dayPillar == ElementType.Fire;
            bool isCautious = dayPillar == ElementType.Earth || dayPillar == ElementType.Metal;
            bool isAnalytical = lifePathNumber == 4 || lifePathNumber == 7;

            var loc = LocalizationManager.Instance;
            if (loc == null) return "...";

            if (isCautious && isAnalytical)
                return loc.Get("fortune.synthesis_cautious_analytical");
            if (isIntuitive && !isAnalytical)
                return loc.Get("fortune.synthesis_intuitive");
            if (isCautious)
                return loc.Get("fortune.synthesis_cautious");
            if (isIntuitive)
                return loc.Get("fortune.synthesis_intuitive_only");

            return loc.Get("fortune.synthesis_default");
        }
    }
}
