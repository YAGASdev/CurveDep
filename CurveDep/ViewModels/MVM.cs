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

        [ObservableProperty]
        public partial int SelectedPointIndex { get; set; } = -1; // -1 = ничего не выбрано

        [ObservableProperty]
        public partial Color[] TableCellColors { get; set; } = CreateDefaultCellColors();

        [ObservableProperty]
        public partial List<CurvePoint> TablePoints { get; set; } = new();

        private static Color[] CreateDefaultCellColors() =>
            Enumerable.Repeat(Color.FromArgb("#9880e5"), 7).ToArray();

        [RelayCommand]
        private void SelectPoint(int index)
        {
            if (index < 0 || index >= 7)
                return;

            SelectedPointIndex = index;

            var colors = CreateDefaultCellColors();
            colors[index] = Color.FromArgb("#2196F3");
            TableCellColors = colors;
        }

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
            SelectedPointIndex = -1;
            TableCellColors = CreateDefaultCellColors();
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
            TableHeaders = GenerateEvenlySpacedValues(L);
        }

        /// <summary>
        /// Генерирует 7 равномерно распределённых значений X от 0 до L включительно.
        /// </summary>
        private string[] GenerateEvenlySpacedValues(double lValue)
        {
            var values = new double[7];

            for (int i = 0; i < 7; i++)
            {
                values[i] = Math.Round(lValue * i / 6.0, 2);
            }

            // Гарантируем точные границы без погрешностей округления
            values[0] = 0;
            values[6] = Math.Round(lValue, 2);

            return values.Select(FormatTableValue).ToArray();
        }

        private static string FormatTableValue(double v) =>
            v % 1 == 0 ? v.ToString("0") : v.ToString("0.##", CultureInfo.InvariantCulture);

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
                TablePoints = xValues.Select(x => new CurvePoint(
                    x,
                    x == 0 ? Math.Round(H1, 2) : _calculator.CalculateHx(x)
                    )).ToList();
            }
            catch (Exception)
            {
                TableData = new[] { "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00" };
                TablePoints = new();
            }
        }
    }
}