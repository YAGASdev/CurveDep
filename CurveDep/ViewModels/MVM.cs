using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace CurveDep.ViewModels
{
    public partial class MVM : ObservableObject
    {
        // Поля для входных данных
        private double _h1 = 9.5;
        public double H1
        {
            get => _h1;
            set => SetProperty(ref _h1, value);
        }

        private double _m1 = 3;
        public double M1
        {
            get => _m1;
            set => SetProperty(ref _m1, value);
        }

        private double _l = 24.3;
        public double L
        {
            get => _l;
            set => SetProperty(ref _l, value);
        }

        private double _kt = 1.21;
        public double Kt
        {
            get => _kt;
            set => SetProperty(ref _kt, value);
        }

        // Поля для результатов расчета
        [ObservableProperty]
        private string beta = "β = 0";

        [ObservableProperty]
        private string deltaLb = "ΔLb = 0, м";

        [ObservableProperty]
        private string deltaLp = "ΔLp = 0, м";

        [ObservableProperty]
        private string q = "q = 0, м/сут";

        [ObservableProperty]
        private string ld = "Lδ = 0, м";

        // Данные для таблицы
        [ObservableProperty]
        private string[] tableHeaders = new[] { "0", "5", "10", "15", "20", "22", "24" };

        [ObservableProperty]
        private string[] tableData = new[] { "0.00", "0.00", "0.00", "0.00", "0.00", "0.00", "0.00" };

        // Вычисленные значения для использования в расчетах
        private double _bv;
        private double _deltaLv;
        private double _deltaLr;
        private double _qValue;
        private double _ldValue;

        [RelayCommand]
        private void Calculate()
        {
            // Обновляем таблицу и выполняем расчеты только по нажатию кнопки
            UpdateTableHeaders();
            PerformCalculations();
            UpdateTableData();
        }

        private void UpdateTableHeaders()
        {
            TableHeaders = GenerateTableValues(L);
        }

        private string[] GenerateTableValues(double lValue)
        {
            // Округляем L до ближайшего целого в меньшую сторону
            int lastValue = (int)Math.Floor(lValue);

            // Для очень маленьких значений используем простой подход
            if (lastValue < 6)
            {
                return GenerateTableForSmallValues(lastValue);
            }

            var values = new HashSet<double> { 0 };

            // Добавляем обязательные значения 5, 10, 15 если они помещаются
            if (5 <= lastValue) values.Add(5);
            if (10 <= lastValue) values.Add(10);
            if (15 <= lastValue) values.Add(15);

            // Добавляем последнее значение
            values.Add(lastValue);

            // Если последнее значение >= 17, добавляем "последнее число минус 1"
            if (lastValue >= 17 && !values.Contains(lastValue - 1))
            {
                values.Add(lastValue - 1);
            }

            // Добавляем промежуточные значения пока не наберем 7
            int maxIterations = 10;
            int iteration = 0;

            while (values.Count < 7 && iteration < maxIterations)
            {
                iteration++;

                // Находим самый большой интервал
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

            // Если все еще меньше 7 значений, добавляем последовательные числа
            if (values.Count < 7)
            {
                for (int i = 0; i <= lastValue && values.Count < 7; i++)
                {
                    values.Add(i);
                }
            }

            // Сортируем и берем ровно 7 значений
            var result = values.OrderBy(v => v).Take(7).ToList();

            // Гарантируем, что последнее значение равно lastValue
            if (result.Count > 0 && result[^1] != lastValue)
            {
                result[^1] = lastValue;
            }

            // Если все еще меньше 7 значений, используем fallback
            if (result.Count < 7)
            {
                return GenerateTableForSmallValues(lastValue);
            }

            return result.Select(v => v.ToString("0")).ToArray();
        }

        private string[] GenerateTableForSmallValues(int lastValue)
        {
            // Для маленьких значений просто создаем последовательность от 0 до lastValue
            var values = new List<double>();

            if (lastValue >= 6)
            {
                // Для значений от 6 и выше
                for (int i = 0; i <= lastValue && values.Count < 7; i++)
                {
                    values.Add(i);
                }
            }
            else
            {
                // Для очень маленьких значений (0-5) используем дробные числа
                double step = lastValue / 6.0;
                for (int i = 0; i < 7; i++)
                {
                    double value = step * i;
                    value = Math.Round(value, 1);
                    values.Add(value);
                }

                // Гарантируем, что последнее значение равно lastValue
                if (values.Count > 0)
                {
                    values[^1] = lastValue;
                }
            }

            // Форматируем вывод
            return values.Select(v => v % 1 == 0 ? v.ToString("0") : v.ToString("0.0")).ToArray();
        }

        private void PerformCalculations()
        {
            try
            {
                // Округляем входные данные до 2 знаков после запятой
                double h1 = Math.Round(H1, 2);
                double m1 = Math.Round(M1, 2);
                double l = Math.Round(L, 2);
                double kt = Math.Round(Kt, 2);

                // Вычисление Bv (β)
                _bv = Math.Round(m1 / (2 * m1 + 1), 2);
                Beta = $"β = {_bv:F2}";

                // Вычисление deltaLv (ΔLb)
                _deltaLv = Math.Round(_bv * h1, 2);
                DeltaLb = $"ΔLb = {_deltaLv:F2}, м";

                // Вычисление deltaLr (ΔLp)
                _deltaLr = Math.Round(l + _deltaLv, 2);
                DeltaLp = $"ΔLp = {_deltaLr:F2}, м";

                // Вычисление q
                _qValue = Math.Round((h1 * h1) / (2 * _deltaLr) * kt, 2);
                Q = $"q = {_qValue:F2}, м/сут";

                // Вычисление Ld (Lδ)
                _ldValue = Math.Round((0.5 * _qValue) / kt, 2);
                Ld = $"Lδ = {_ldValue:F2}, м";
            }
            catch (Exception)
            {
                // В случае ошибки сбрасываем значения
                Beta = "β = 0";
                DeltaLb = "ΔLb = 0, м";
                DeltaLp = "ΔLp = 0, м";
                Q = "q = 0, м/сут";
                Ld = "Lδ = 0, м";
            }
        }

        private double CalculateHx(double x)
        {
            try
            {
                double result = Math.Sqrt(2 * (_qValue / Kt) * (L - x + _ldValue));
                return Math.Round(result, 2);
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        private void UpdateTableData()
        {
            try
            {
                // Преобразуем заголовки таблицы в числа для расчетов
                var xValues = TableHeaders.Select(header =>
                    double.Parse(header, CultureInfo.InvariantCulture)).ToArray();

                var newTableData = new string[7];

                for (int i = 0; i < xValues.Length && i < 7; i++)
                {
                    double x = xValues[i];
                    double hx = CalculateHx(x);
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