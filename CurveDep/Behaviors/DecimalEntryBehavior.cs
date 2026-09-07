using System.Globalization;
using Microsoft.Maui.Controls;

namespace CurveDep.Behaviors
{
    public class DecimalEntryBehavior : Behavior<Entry>
    {
        private bool _isUpdatingText = false;

        protected override void OnAttachedTo(Entry entry)
        {
            entry.TextChanged += OnEntryTextChanged;
            base.OnAttachedTo(entry);
        }

        protected override void OnDetachingFrom(Entry entry)
        {
            entry.TextChanged -= OnEntryTextChanged;
            base.OnDetachingFrom(entry);
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs args)
        {
            if (_isUpdatingText) return;

            if (sender is Entry entry)
            {
                string newText = args.NewTextValue;

                // Разрешаем пустую строку
                if (string.IsNullOrWhiteSpace(newText))
                    return;

                // Заменяем точку на запятую, но только если нет существующей запятой
                if (newText.Contains('.') && !newText.Contains(','))
                {
                    _isUpdatingText = true;
                    newText = newText.Replace('.', ',');
                    entry.Text = newText;
                    _isUpdatingText = false;
                    return;
                }

                // Проверяем, является ли ввод числом с запятой и максимум 2 знаками после запятой
                if (!IsValidDecimalInput(newText))
                {
                    _isUpdatingText = true;
                    // Возвращаем предыдущее значение
                    entry.Text = args.OldTextValue;
                    _isUpdatingText = false;
                }
            }
        }

        private bool IsValidDecimalInput(string input)
        {
            // Разрешаем цифры и одну запятую
            bool hasComma = false;
            int digitsAfterComma = 0;

            foreach (char c in input)
            {
                if (c == ',')
                {
                    if (hasComma) return false;
                    hasComma = true;
                }
                else if (!char.IsDigit(c))
                {
                    return false;
                }
                else if (hasComma)
                {
                    digitsAfterComma++;
                    if (digitsAfterComma > 2) return false;
                }
            }

            // Проверяем, что запятая не в начале
            if (input.StartsWith(","))
                return false;

            return true;
        }
    }
}