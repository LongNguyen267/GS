using System.Text;
using System.Text.RegularExpressions;

namespace GameStore.Helpers
{
    public static class SlugHelper
    {
        public static string GenerateSlug(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return "";

            string str = RemoveSign4VietnameseString(phrase.ToLower());

            // Thay khoảng trắng thành gạch ngang
            str = Regex.Replace(str, @"\s+", "-");
            // Loại bỏ ký tự đặc biệt
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            // Loại bỏ gạch ngang thừa (nếu có 2 gạch liền nhau)
            str = Regex.Replace(str, @"\s+", " ").Trim();

            return str;
        }

        private static string RemoveSign4VietnameseString(string str)
        {
            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
            }
            return str;
        }

        private static readonly string[] VietnameseSigns = new string[]
        {
            "aAeEoOuUiIdDyY",
            "áàạảãâấầậẩẫăắằặẳẵ",
            "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
            "éèẹẻẽêếềệểễ",
            "ÉÈẸẺẼÊẾỀỆỂỄ",
            "óòọỏõôốồộổỗơớờợởỡ",
            "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
            "úùụủũưứừựửữ",
            "ÚÙỤỦŨƯỨỪỰỬỮ",
            "íìịỉĩ",
            "ÍÌỊỈĨ",
            "đ",
            "Đ",
            "ýỳỵỷỹ",
            "ÝỲỴỶỸ"
        };
    }
}