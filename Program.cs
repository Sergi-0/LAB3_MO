using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace simplex_method2
{
    public class SMX_MD
    {
        private double[] c; // массив содержащий коэфициенты при переменных в функции F.
        private double[,] A; // матрица содержащая коэфициенты при переменны в неравенствах. [строка, столбец]
        private double[] b; // массив содержащий числа, которые стоят в правых частях неравенств.
        private double[,] table; // таблица которую мы будем преобразовывать. [столбец, строка]
        private string[] FreeX; // самая верхняя строчка в таблице, которая содержит свободные иксы.
        private string[] DependX; // самый левый столбец содержащий зависимые иксы.
        private string minormax; // поле содержащее строку min или max, в зависимости от того, какое значение мы будем искать у функции F.

        private int coord_pred_razr_e_st = -1; // вспомогательное поле, содержащее столбец разрешающего элемента на предыдущей итерации
        private int coord_pred_razr_e_str = -1; // вспомогательное поле, содержащее строку разрешающего элемента на предыдущей итерации

        public SMX_MD(double[] c, double[,] A, double[] b, string minormax)
        {
            if (A.GetLength(0) != b.Length || A.GetLength(1) != c.Length - 1) // проверка чтобы размеры массивов c и b, и матрицы A соответствовали друг другу.
            {
                throw new Exception("Неверное соотношение размеров матриц!");
            }

            if (minormax == "min") this.c = c;
            else
            {
                double[] c_temp = new double[c.Length]; // создается временный массив c_temp, который будет заполнен коэфициентами из масива c, знак коэфициентов будет зависить от того ищем мы max или min функции F.
                for (int i = 0; i < c.Length; i++) { c_temp[i] = -1 * c[i]; }
                this.c = c_temp;
            }
            this.A = A;
            this.b = b;
            this.minormax = minormax;
            FillTable();

            FreeX = new string[c.Length - 1];
            int k = 0; // заполнение свободных иксов. Счетчик k понадобится для того, чтобы знать сколько было зависимых иксов. Это нужно чтобы правильно именовать зависимые иксы.
            for (int i = 0; i < c.Length - 1; i++)
            {
                FreeX[i] = $"X{i + 1}";
                k++;
            }

            DependX = new string[b.Length]; // заполнение зависимых иксов
            for (int i = 0; i < b.Length; i++)
            {
                k++;
                DependX[i] = $"X{k}";
            }
        }

        public bool Solution()
        {
            Console.WriteLine("Начальная задача: ");
            Console.WriteLine();
            Print();
            Console.WriteLine();
            Console.WriteLine();

            if (FindOptSolve()) { if (minormax == "max") { table[0, b.Length] = -1 * table[0, b.Length]; Print(); Console.WriteLine(); Console.WriteLine(); } Check(); return true; }
            return false;
        }

        public bool FindOptSolve()
        {
            if (FindOprSolve())
            {
                int l = 0; // счетчик отрицательных элементов в строке F
                for (int t = 1; t < c.Length; t++) if (table[t, b.Length] <= 0) { l++; }
                if (l == c.Length - 1) { return true; } // если все элементы в строке F отрицательные и найдено опорное решение, то мы нашли оптимальное решение

                for (int i = 1; i < c.Length; i++)
                {
                    if (table[i, b.Length] > 0)
                    {
                        int razr_stolb = i;

                        double min = double.MaxValue; // поиск разрешающей строки. Переменная min будет содержать минимальное положительное число. Для начального значение возьмем очень большое положительное число.

                        int flag2 = 0; // флаг показывает, меняли ли мы переменную min, если flag2 > 0, значит меняли.
                        int razr_str = -1;

                        for (int z = 0; z < b.Length; z++)
                        {
                            double k = table[0, z] / table[razr_stolb, z];
                            if (k < min && k > 0)
                            {
                                min = k;
                                razr_str = z;
                                flag2++;
                            }
                        }

                        if (flag2 == 0) { continue; }

                        if (razr_str != coord_pred_razr_e_str || razr_stolb != coord_pred_razr_e_st)
                        {
                            coord_pred_razr_e_st = razr_stolb;
                            coord_pred_razr_e_str = razr_str;
                            fix_table(razr_str, razr_stolb);
                        }
                        else continue;

                        return FindOptSolve();
                    }
                }
            }

            return false;
        }

        public void FillTable() // начальное заполнение таблицы, используется только в конструкторе
        {
            double[,] table1 = new double[c.Length, b.Length + 1];

            for (int i = 0; i < b.Length; i++) { table1[0, i] = b[i]; } // заполнение первой колонки начальной таблицы
            table1[0, b.Length] = c[c.Length - 1];

            for (int i = 0; i < b.Length; i++)
            {
                for (int j = 1; j < c.Length; j++)
                {
                    table1[j, i] = A[i, j - 1];
                }
            }

            for (int i = 1; i < c.Length; i++) { table1[i, b.Length] = -c[i - 1]; } // на этом моменте заполнили всю начальную таблицу
            table = table1;
        }

        public bool FindOprSolve() // поиск опорного решения
        {
            int l = 0; // счетчик положительных элементов в столбце свободных членов
            for (int t = 0; t < b.Length; t++) if (table[0, t] >= 0) { l++; }
            if (l == b.Length) { return true; }

            for (int i = 0; i < b.Length; i++)
            {
                if (table[0, i] < 0)
                {
                    for (int j = 1; j < c.Length; j++)
                    {
                        if (table[j, i] < 0)
                        {
                            int razr_stolb = j;

                            double min = double.MaxValue; // поиск разрешающей строки. Переменная min будет содержать минимальное положительное число. Для начального значение возьмем очень большое положительное число.

                            int flag1 = 0; // флаг показывает, меняли ли мы переменную min, если flag1 > 0, значит меняли.
                            int razr_str = -1;

                            for (int z = 0; z < b.Length; z++)
                            {
                                double k = table[0, z] / table[razr_stolb, z];
                                if (k < min && k > 0)
                                {
                                    min = k;
                                    razr_str = z;
                                    flag1++;
                                }
                            }

                            if (flag1 == 0) { continue; }

                            if (razr_str != coord_pred_razr_e_str || razr_stolb != coord_pred_razr_e_st)
                            {
                                coord_pred_razr_e_st = razr_stolb;
                                coord_pred_razr_e_str = razr_str;
                                fix_table(razr_str, razr_stolb);
                            }
                            else continue;

                            return FindOprSolve();
                        }
                    }
                }
            }

            return false;
        }

        public void fix_table(int razr_str, int razr_stolb) // перестройка таблицы при заданном разрешающем элементе
        {
            double[,] table1 = new double[c.Length, b.Length + 1];

            double r_e = table[razr_stolb, razr_str]; // разрешающий элемент

            string _x = FreeX[razr_stolb - 1];
            FreeX[razr_stolb - 1] = DependX[razr_str];
            DependX[razr_str] = _x;

            table1[razr_stolb, razr_str] = 1 / r_e;

            for (int i = 0; i < c.Length; i++)
            {
                if (i != razr_stolb) table1[i, razr_str] = table[i, razr_str] / r_e;
            }

            for (int i = 0; i < b.Length + 1; i++)
            {
                if (i != razr_str) table1[razr_stolb, i] = -table[razr_stolb, i] / r_e;
            }

            for (int i = 0; i < b.Length + 1; i++)
            {
                for (int j = 0; j < c.Length; j++)
                {
                    if (i != razr_str && j != razr_stolb) table1[j, i] = table[j, i] - (table[razr_stolb, i] * table[j, razr_str]) / r_e;
                }
            }

            table = table1;

            Print();
            Console.WriteLine();
            Console.WriteLine();
        }

        public void Print() // выводит таблицу
        {
            Console.Write("\t");
            Console.Write(" S" + "\t");
            foreach (object obj in FreeX) { Console.Write(" " + obj + "\t"); }
            Console.WriteLine();
            Console.WriteLine();

            for (int i = 0; i < b.Length + 1; i++)
            {
                if (i != b.Length) Console.Write(DependX[i] + "\t"); else Console.Write("F" + "\t");

                for (int j = 0; j < c.Length; j++)
                {
                    double okr = Math.Abs(table[j, i]) < 1e-2 ? 0 : Math.Round(table[j, i], 2); // округленное число
                    if (okr >= 0) Console.Write(" " + okr + "\t"); // этот if нужен для выравнивания столбцов.
                    else Console.Write(okr + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine("______________________________________________");
        }

        public void Check() // проверка найденного решение методом подстановки в начальные условия / можно в будущем добавить округление(-0)
        {
            Console.WriteLine("Проверка решения: ");
            Console.WriteLine();
            Console.WriteLine("Функция: ");
            double[] solve_x = new double[table.GetLength(0) + table.GetLength(1) - 2]; // создаем массив иксов, которые решают поставленную задачу, берем их из последней таблицы.
            string[] str_X = new string[solve_x.Length]; // вспомогательный массив для определения последовательности иксов в массиве solve_x, взятых из последней таблицы table;
            for (int i = 0; i < solve_x.Length; i++) str_X[i] = $"X{i + 1}";

            for (int i = 0; i < DependX.Length; i++)
            {
                int k = Array.IndexOf(str_X, DependX[i]); // переменная для определения номера икса в массиве solve_x
                solve_x[k] = table[0, i];
            }

            for (int i = 0; i < c.Length; i++)
            {
                if (minormax == "min" && i != c.Length - 1) Console.Write($"{c[i]}*{solve_x[i]} + ");
                if (minormax == "min" && i == c.Length - 1) Console.Write($"{c[i]} = {table[0, table.GetLength(1) - 1]}");
                if (minormax == "max" && i != c.Length - 1) Console.Write($"{-c[i]}*{solve_x[i]} + ");
                if (minormax == "max" && i == c.Length - 1) Console.Write($"{-c[i]} = {table[0, table.GetLength(1) - 1]}");
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Неравенства: ");

            for (int i = 0; i < b.Length; i++)
            {
                for (int j = 0; j < c.Length - 1; j++)
                {
                    if (j != c.Length - 2) Console.Write($"{A[i, j]}*{solve_x[j]} + ");
                    if (j == c.Length - 2) Console.Write($"{A[i, j]}*{solve_x[j]} <= {b[i]}");
                }
                Console.WriteLine();
            }
            Console.WriteLine("______________________________________________");
            Console.WriteLine();
        }

        public double[] give_solve()
        {
            double[] solve_x = new double[c.Length]; // создаем массив иксов, которые решают поставленную задачу, берем их из последней таблицы.
            string[] str_X = new string[solve_x.Length]; // вспомогательный массив для определения последовательности иксов в массиве solve_x, взятых из последней таблицы table;
            for (int i = 0; i < c.Length; i++) str_X[i] = $"X{i + 1}";

            for (int i = 0; i < DependX.Length; i++)
            {
                int k = Array.IndexOf(str_X, DependX[i]); // переменная для определения номера икса в массиве solve_x
                if (k >= 0) solve_x[k] = table[0, i];
            }
            
            return solve_x;
        }

        public bool IntSM() // IntSM - Integer Simplex Method
        {
            Console.WriteLine("Узел:");
            if (!Solution()) { Console.WriteLine("Решение этого узла не найдено(даже в дробном виде)"); Console.WriteLine("______________________________________________"); return false; };

            double[] dep_x = give_solve(); // решение симплекс метода на данном этапе
            double x1 = 0; // первое попавшееся в решении дробное число
            int k = -1; // позиция первого найденного дробного икса
            for(int i = 0;i < c.Length-1; i++)
            {
                if(dep_x[i] != Math.Floor(dep_x[i])) { 
                    x1 = dep_x[i]; 
                    k = i;
                    break; 
                }
            }

            if (k == -1)
            {
                Console.WriteLine();
                Console.WriteLine("Решение узла: ");
                Console.WriteLine();
                for (int i = 0; i < c.Length-1; i++) Console.Write($"X{i+1}: {dep_x[i]} "); 
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine($"Значение функции: {table[0, b.Length]}");
                Console.WriteLine("______________________________________________");
                Console.WriteLine();
                return true;    // все иксы целочисленные
            }

            double x1_floor = Math.Floor(x1); // для new_A1
            double[,] new_A1 = new double[A.GetLength(0)+1, A.GetLength(1)];
            double[] new_b1 = new double[b.Length + 1];
            double[] new_c = new double[c.Length];
            if (minormax == "min") new_c = c;
            else
            {
                double[] c_temp = new double[c.Length];
                for (int i = 0; i < c.Length; i++) { c_temp[i] = -1 * c[i]; }
                new_c = c_temp;
            }
            for (int i = 0; i < A.GetLength(0); i++)    for (int j = 0; j < A.GetLength(1); j++)    new_A1[i, j] = A[i, j];
            for (int i = 0; i < b.Length; i++) new_b1[i] = b[i];
            new_A1[A.GetLength(0),k] = 1; // 1 т.к тут будет знак <=
            new_b1[b.Length] = x1_floor;
            bool bool1 = new SMX_MD(new_c, new_A1, new_b1, minormax).IntSM();



            double x1_upward = Math.Floor(x1) + 1; // для new A2
            double[,] new_A2 = new double[A.GetLength(0)+1, A.GetLength(1)];
            double[] new_b2 = new double[b.Length + 1];
            for(int i = 0;i < A.GetLength(0); i++)  for(int j = 0;j < A.GetLength(1); j++) new_A2[i,j] = A[i,j];
            for(int i = 0; i < b.Length; i++) new_b2[i] = b[i];
            new_A2[A.GetLength(0), k] = -1; // -1 т.к тут будет знак >= и мы будем домножать неравенство на -1
            new_b2[b.Length] = -x1_upward; // -1 т.к тут будет знак >= и мы будем домножать неравенство на -1
            bool bool2 = new SMX_MD(new_c, new_A2, new_b2, minormax).IntSM();

            if(bool1 || bool2) return true; else return false;
        }

        public void perebor() // для 3 переменных
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Метод полного перебора: ");
            Console.WriteLine();

            double max_otn = double.MinValue;

            for(int i = 0; i < A.GetLength(0); i++)
            {
                for(int j = 0; j < A.GetLength(1);j++)
                {
                    if (A[i,j] != 0)
                    {
                        double otn = b[i] / A[i, j];
                        if(otn>max_otn) max_otn = otn;
                    }
                    
                }
            }

            max_otn = Math.Floor(max_otn)+1;

            for(int x1 = 0;x1 < max_otn; x1++)
            {
                for(int x2 = 0; x2 < max_otn; x2++)
                {
                    for(int x3 = 0; x3 < max_otn; x3++)
                    {
                        bool t = true;

                        for (int i = 0; i < A.GetLength(0); i++)
                        {
                            t = A[i, 0] * x1 + A[i, 1] * x2 + A[i, 2] * x3 <= b[i];
                            if (!t) break;
                        }
                        
                        if(t)
                        {
                            double[] new_c;
                            if (minormax == "min") new_c = c;
                            else
                            {
                                double[] c_temp = new double[c.Length]; // создается временный массив c_temp, который будет заполнен коэфициентами из масива c, знак коэфициентов будет зависить от того ищем мы max или min функции F.
                                for (int i = 0; i < c.Length; i++) { c_temp[i] = -1 * c[i]; }
                                new_c = c_temp;
                            }
                            Console.WriteLine($"Значение функции: {new_c[0] * x1 + new_c[1] * x2 + new_c[2] * x3}");
                            Console.WriteLine($"Значение переменных: X1: {x1}, X2: {x2}, X3: {x3}");
                            Console.WriteLine("______________________________________________");
                        }
                    }
                }
            }
        }
    }
    internal class Program1
    {
        static void Main(string[] args)
        {

            double[] c = new double[] { 7, 8, 3, 0 }; // последним идет свободный член, его надо указывать даже если он 0 !!!
            double[] b = new double[] { 4,7,8 };
            double[,] A = new double[,] { { 3, 1, 1 }, { 1, 4, 0 }, { 0, 0.5, 2 } };

            SMX_MD t = new SMX_MD(c, A, b, "max");
            t.IntSM();
            t.perebor();
        }
    }
} 