using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CurveDep.Models;
using CurveDep.Services;
using System.Globalization;

namespace CurveDep.ViewModels
{
    public partial class MVM : ObservableObject
    {
        private readonly SeepageCurveCalculator _calculator = new();

        public MVM()
        {
            // Выполняем первый расчёт сразу при запуске, чтобы график и профиль
            // отображались корректно ещё до первого нажатия "Продолжить".
            Calculate();
        }

        // ===== Входные данные (изменяются пользователем "вживую") =====

        [ObservableProperty]
        public partial double H1 { get; set; } = 9.5;

        [ObservableProperty]
        public partial double M1 { get; set; } = 3;

        [ObservableProperty]
        public partial double L { get; set; } = 24.3;

        [ObservableProperty]
        public partial double Kt { get; set; } = 1.21;

        // ===== "Снимок" данных, зафиксированный на момент последнего успешного расчёта =====
        // Используется для отрисовки профиля плотины и кривой, чтобы они не рассинхронизировались.

        [ObservableProperty]
        public partial double DrawH1 { get; set; }

        [ObservableProperty]
        public partial double DrawM1 { get; set; }

        [ObservableProperty]
        public partial double DrawL { get; set; }

        // ===== Результаты расчёта (чистые числа, форматирование — в XAML) =====

        [ObservableProperty]
        public partial double BetaValue { get; set; }

        [ObservableProperty]
        public partial double DeltaLbValue { get; set; }

        [ObservableProperty]
        public partial double DeltaLpValue { get; set; }

        [ObservableProperty]
        public partial double QValue { get; set; }

        [ObservableProperty]
        public partial double LdValue { get; set; }

        [ObservableProperty]
        public partial List<CurvePoint> CurvePoints { get; set; } = new();

        // ===== Валидация =====

        [ObservableProperty]
        public partial string ErrorMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool HasError { get; set; }

        // ===== Данные для таблицы =====

        [ObservableProperty]
        public partial string[] TableHeaders { get; set; } =
            new[] { "0", "5", "10", "15", "20", "22", "24" };

        [ObservableProperty]
        public partial string[] TableData { get; set; } =
            new[] { "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00" };

        [RelayCommand]
        private void Calculate()
        {
            if (!ValidateInputs(out string error))
            {
                ErrorMessage = error;
                HasError = true;
                return;
            }

            HasError = false;
            ErrorMessage = string.Empty;

            // Фиксируем "снимок" входных данных для согласованной отрисовки
            DrawH1 = H1;
            DrawM1 = M1;
            DrawL = L;

            UpdateTableHeaders();
            PerformCalculations();
            UpdateTableData();
        }

        private bool ValidateInputs(out string error)
        {
            if (H1 <= 0)
            {
                error = "Глубина воды Н1 должна быть больше нуля.";
                return false;
            }

            if (M1 <= 0)
            {
                error = "Заложение откоса m1 должно быть больше нуля.";
                return false;
            }

            if (L <= 0)
            {
                error = "Длина плотины L должна быть больше нуля.";
                return false;
            }

            if (Kt <= 0)
            {
                error = "Коэффициент фильтрации Kt должен быть больше нуля.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void UpdateTableHeaders()
        {
            TableHeaders = GenerateTableValues(L);
        }

        private string[] GenerateTableValues(double lValue)
        {
            int lastValue = (int)Math.Floor(lValue);

            if (lastValue < 6)
            {
                return GenerateTableForSmallValues(lastValue);
            }

            var values = new HashSet<double> { 0 };

            if (5 <= lastValue) values.Add(5);
            if (10 <= lastValue) values.Add(10);
            if (15 <= lastValue) values.Add(15);

            values.Add(lastValue);

            if (lastValue >= 17 && !values.Contains(lastValue - 1))
            {
                values.Add(lastValue - 1);
            }

            int maxIterations = 10;
            int iteration = 0;

            while (values.Count < 7 && iteration < maxIterations)
            {
                iteration++;

                var sortedValues = values.OrderBy(v => v).ToList();
                double maxGap = 0;
                int gapIndex = -1;

                for (int i = 0; i < sortedValues.Count - 1; i++)
                {
                    double gap = sortedValues[i + 1] - sortedValues[i];
                    if (gap > maxGap && gap >= 1)
                    {
                        maxGap = gap;
                        gapIndex = i;
                    }
                }

                if (gapIndex >= 0)
                {
                    double newValue = Math.Round((sortedValues[gapIndex] + sortedValues[gapIndex + 1]) / 2);
                    if (newValue > sortedValues[gapIndex] && newValue < sortedValues[gapIndex + 1])
                    {
                        values.Add(newValue);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            if (values.Count < 7)
            {
                for (int i = 0; i <= lastValue && values.Count < 7; i++)
                {
                    values.Add(i);
                }
            }

            var result = values.OrderBy(v => v).Take(7).ToList();

            if (result.Count > 0 && result[^1] != lastValue)
            {
                result[^1] = lastValue;
            }

            if (result.Count < 7)
            {
                return GenerateTableForSmallValues(lastValue);
            }

            return result.Select(v => v.ToString("0")).ToArray();
        }

        private string[] GenerateTableForSmallValues(int lastValue)
        {
            var values = new List<double>();

            if (lastValue >= 6)
            {
                for (int i = 0; i <= lastValue && values.Count < 7; i++)
                {
                    values.Add(i);
                }
            }
            else
            {
                double step = lastValue / 6.0;
                for (int i = 0; i < 7; i++)
                {
                    double value = step * i;
                    value = Math.Round(value, 1);
                    values.Add(value);
                }

                if (values.Count > 0)
                {
                    values[^1] = lastValue;
                }
            }

            return values.Select(v => v % 1 == 0 ? v.ToString("0") : v.ToString("0.0")).ToArray();
        }

        private void PerformCalculations()
        {
            try
            {
                _calculator.H1 = H1;
                _calculator.M1 = M1;
                _calculator.L = L;
                _calculator.Kt = Kt;

                var result = _calculator.Calculate();

                BetaValue = result.Beta;
                DeltaLbValue = result.DeltaLb;
                DeltaLpValue = result.DeltaLp;
                QValue = result.Q;
                LdValue = result.Ld;

                CurvePoints = _calculator.BuildCurve(60);
            }
            catch (Exception)
            {
                BetaValue = 0;
                DeltaLbValue = 0;
                DeltaLpValue = 0;
                QValue = 0;
                LdValue = 0;
                CurvePoints = new();
            }
        }

        private void UpdateTableData()
        {
            try
            {
                var xValues = TableHeaders.Select(header =>
                    double.Parse(header, CultureInfo.InvariantCulture)).ToArray();

                var newTableData = new string[7];

                for (int i = 0; i < xValues.Length && i < 7; i++)
                {
                    double x = xValues[i];
                    double hx = _calculator.CalculateHx(x);
                    newTableData[i] = hx.ToString("F2", CultureInfo.InvariantCulture);
                }

                TableData = newTableData;
            }
            catch (Exception)
            {
                TableData = new[] { "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00" };
            }
        }
    }
}