using System;
using UnityEngine;

namespace FPSTrump.Manager
{
    /// <summary>
    /// プレイヤー生年月日の保存・取得・バリデーション
    /// PlayerPrefsで永続化（APIKeyManagerと同じパターン）
    /// </summary>
    public class PlayerBirthdayManager : MonoBehaviour
    {
        public static PlayerBirthdayManager Instance { get; private set; }

        private const string KEY_BIRTH_YEAR = "PlayerBirthYear";
        private const string KEY_BIRTH_MONTH = "PlayerBirthMonth";
        private const string KEY_BIRTH_DAY = "PlayerBirthDay";

        private int birthYear;
        private int birthMonth;
        private int birthDay;
        private bool hasData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBirthday();
        }

        /// <summary>
        /// 生年月日を保存
        /// </summary>
        public void SaveBirthday(int year, int month, int day)
        {
            if (!IsValidDate(year, month, day))
            {
                Debug.LogWarning($"[PlayerBirthdayManager] Invalid date: {year}/{month}/{day}");
                return;
            }

            birthYear = year;
            birthMonth = month;
            birthDay = day;
            hasData = true;

            PlayerPrefs.SetInt(KEY_BIRTH_YEAR, year);
            PlayerPrefs.SetInt(KEY_BIRTH_MONTH, month);
            PlayerPrefs.SetInt(KEY_BIRTH_DAY, day);
            PlayerPrefs.Save();

            Debug.Log($"[PlayerBirthdayManager] Birthday saved: {GetBirthdayString()}");
        }

        /// <summary>
        /// 保存済みの生年月日をロード
        /// </summary>
        private void LoadBirthday()
        {
            if (PlayerPrefs.HasKey(KEY_BIRTH_YEAR) &&
                PlayerPrefs.HasKey(KEY_BIRTH_MONTH) &&
                PlayerPrefs.HasKey(KEY_BIRTH_DAY))
            {
                birthYear = PlayerPrefs.GetInt(KEY_BIRTH_YEAR);
                birthMonth = PlayerPrefs.GetInt(KEY_BIRTH_MONTH);
                birthDay = PlayerPrefs.GetInt(KEY_BIRTH_DAY);
                hasData = IsValidDate(birthYear, birthMonth, birthDay);

                if (hasData)
                    Debug.Log($"[PlayerBirthdayManager] Birthday loaded: {GetBirthdayString()}");
            }
            else
            {
                // デフォルト値: 1989/02/28
                birthYear = 1989;
                birthMonth = 2;
                birthDay = 28;
                hasData = true;
                Debug.Log($"[PlayerBirthdayManager] Using default birthday: {GetBirthdayString()}");
            }
        }

        /// <summary>
        /// 生年月日が設定済みか
        /// </summary>
        public bool HasBirthday() => hasData;

        /// <summary>
        /// 生年月日を取得 (year, month, day)
        /// </summary>
        public (int year, int month, int day) GetBirthday()
        {
            return hasData ? (birthYear, birthMonth, birthDay) : (0, 0, 0);
        }

        /// <summary>
        /// YYYY/MM/DD形式の文字列を取得
        /// </summary>
        public string GetBirthdayString()
        {
            if (!hasData) return "";
            return $"{birthYear:D4}/{birthMonth:D2}/{birthDay:D2}";
        }

        /// <summary>
        /// 年齢を計算
        /// </summary>
        public int GetAge()
        {
            if (!hasData) return -1;

            var today = DateTime.Today;
            int age = today.Year - birthYear;
            if (today.Month < birthMonth || (today.Month == birthMonth && today.Day < birthDay))
                age--;
            return age;
        }

        /// <summary>
        /// 日付のバリデーション
        /// </summary>
        private bool IsValidDate(int year, int month, int day)
        {
            if (year < 1900 || year > DateTime.Now.Year) return false;
            if (month < 1 || month > 12) return false;
            if (day < 1) return false;

            int daysInMonth = DateTime.DaysInMonth(year, month);
            return day <= daysInMonth;
        }

        /// <summary>
        /// 生年月日をクリア（デバッグ用）
        /// </summary>
        [ContextMenu("Clear Birthday")]
        public void ClearBirthday()
        {
            PlayerPrefs.DeleteKey(KEY_BIRTH_YEAR);
            PlayerPrefs.DeleteKey(KEY_BIRTH_MONTH);
            PlayerPrefs.DeleteKey(KEY_BIRTH_DAY);
            PlayerPrefs.Save();
            hasData = false;
            Debug.Log("[PlayerBirthdayManager] Birthday cleared");
        }
    }
}
