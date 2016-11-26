using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using CenterSpace.NMath.Analysis;
using CenterSpace.NMath.Core;

namespace CoreData
{
    public enum DeseaseType
    {
        dtUnknown = 0,
        dtCiroz = 1,
        dtPochki = 2,
        dtReanimation = 3,
    }

    public enum ParamType
    {
        ptQ = 1,
        ptTC = 2,
        ptISO = 3,
    }

    
    public class NodeData
    {
        public NodeData(int id, double low, double high, DeseaseType destype, ParamType paramType)
        {
            this.Id = id;            
            this.Low = low;
            this.High = high;
            this.DesType = destype;
            this.ParamType = paramType;
        }
        public int Id;
        public double High;
        public double Low;
        public DeseaseType DesType;
        public ParamType ParamType;

        public double GetAverage()
        {
            return ((High + Low)/2);
        }
    }

    public class DataStorage
    {
        public bool Init()
        {
            NodeData.Clear();
            NodeData = SqliteWorker.LoadFromJson();
            return NodeData.Count > 0;
        }

        public List<NodeData> NodeData { get; set; } = new List<NodeData>();
        public List<Point> SpmVals { get; set; }= new List<Point>();

        double q = 0;
        double tc = 0;
        double iso = 0;
        DeseaseType currentDes;
        double BestA = 0.0;
        double BestB = 0.0;
        double BestC = 0.0;

        public void HandleCsv(string filename)
        {            
            if (!LoadDataFromCsv(filename))
                return;

            if (!CalculateParams())
                return;

            ResizeBounds();
            SqliteWorker.SaveToJson(NodeData);
        }


        public DeseaseType AnalyzeFile(string filename, Dictionary<double, double> Kcalc)
        {
            // загружаем
            if (!LoadDataFromCsv(filename, false))
                return DeseaseType.dtUnknown;

            // считаем
            if (!CalculateParams())
                return DeseaseType.dtUnknown;

            // вернем рассчетную функцию (экспоненциальную)
            foreach (var pt in SpmVals)
            {
                double Kr = BestC + BestA * Math.Exp(BestB * pt.X);
                Kcalc.Add(pt.X, Kr);
            }

            // Анализируем

            // хранит индексы болезни по каждому параметру
            int[] desIndexes = new int[3];
            for (int i=1; i<=3; ++i)
            {
                // формируем оценку по каждому отдельному параметру
                var nodesByDes = NodeData.Where(obj => (obj.ParamType == (ParamType)i));
                double param = 0.0;
                switch ((ParamType)i)
                {
                    case ParamType.ptISO:
                        param = iso;
                        break;
                    case ParamType.ptQ:
                        param = q;
                        break;
                    case ParamType.ptTC:
                        param = tc;
                        break;
                }
                double minval = double.MaxValue;
                int desidx = 0;
                int cnt = 1;
                foreach (var node in nodesByDes)
                {
                    if (Math.Abs(param - node.GetAverage()) < minval)
                    {
                        minval = Math.Abs(param - node.GetAverage());
                        desidx = cnt;
                    }                    
                    cnt++;
                }
                desIndexes[i] = i;
            }
            // ищем наиболее часто встречающееся
            var most = desIndexes.GroupBy(x => x).OrderByDescending(x => x.Count()).First();
            if (most.Count() == 1)
                return DeseaseType.dtUnknown;

            return (DeseaseType) most.Key;
        }

        void ResizeBounds()
        {
            var node = NodeData.FirstOrDefault(obj => (obj.DesType == currentDes) && (obj.ParamType == ParamType.ptISO));
            if (node == null)
                return;
            node.High = Math.Max(node.High, iso);
            node.Low = Math.Min(node.Low, iso);

            node = NodeData.FirstOrDefault(obj => (obj.DesType == currentDes) && (obj.ParamType == ParamType.ptQ));
            if (node == null)
                return;
            node.High = Math.Max(node.High, q);
            node.Low = Math.Min(node.Low, q);

            node = NodeData.FirstOrDefault(obj => (obj.DesType == currentDes) && (obj.ParamType == ParamType.ptTC));
            if (node == null)
                return;
            node.High = Math.Max(node.High, tc);
            node.Low = Math.Min(node.Low, tc);
        }

        private bool CalculateParams()
        {            
            if (!CalcQ())
                return false;

            if (!CalcTC())
                return false;

            if (!CalcISO())
                return false;
            return true;
        }

        private bool CalcQ()
        {
            BestA = 0.0;
            BestB = 0.0;
            BestC = 0.0;

            var f = new DoubleParameterizedDelegate(AnalysisFunctions.ThreeParameterExponential);
            var fitter = new OneVariableFunctionFitter<TrustRegionMinimizer>(f);

            var lvect = new DoubleVector();
            var kvect = new DoubleVector();
            foreach (var pt in SpmVals)
            {
                lvect.Append(pt.X);
                kvect.Append(pt.Y);
            }
            var start = new DoubleVector(0.1, 0.1, 0.1);
            DoubleVector solution = fitter.Fit(lvect, kvect, start);
            BestA = solution[0];
            BestB = solution[1];
            BestC = solution[2];
            q = BestA;
            return true;
        }
        private bool CalcTC()
        {
            tc = 0;
            
            foreach (var pt in SpmVals)
            {
                double l = pt.X;
                double Ke = pt.Y;
                double Kr = BestC + BestA * Math.Exp(BestB * l);
                if (Ke == 0)
                {
                    continue;
                }
                tc = tc + Math.Abs((Ke - Kr) / Ke);                
            }
            tc = tc*100;
            return true;
        }
        private bool CalcISO()
        {
            iso = 0;
            for (var i=0; i < SpmVals.Count - 1; ++i)
            {
                var p1 = SpmVals[i];
                var p2 = SpmVals[i + 1];

                var h = p2.X - p1.X;
                var S = (p1.Y + p2.Y)*h/2;
                iso = iso + S;
            }
            return true;
        }

        public bool LoadDataFromCsv(string filename, bool doReadDesease = true)
        {
            SpmVals.Clear();
            using (var reader = new StreamReader(File.OpenRead(filename)))
            {
                // болезнь
                if (doReadDesease)
                {
                    var desStr = reader.ReadLine();
                    currentDes = (DeseaseType)Convert.ToInt32(desStr);
                }                

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line?.Split(';');
                    if (values?.Length < 2)
                        continue;

                    double[] doubles;
                    bool valid = TryConvertToDoubleArr(values, out doubles);
                    if (valid)
                        SpmVals.Add(new Point(doubles[0], doubles[1]));
                }                
            }            
            return true;
        }

        bool TryConvertToDoubleArr(string[] arr, out double[] doubles)
        {
            doubles = new double[2];
            try
            {
                double dval;
                double.TryParse(arr[0], out dval);
                doubles[0] = dval;
                double.TryParse(arr[1], out dval);                
                doubles[1] = dval;
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
