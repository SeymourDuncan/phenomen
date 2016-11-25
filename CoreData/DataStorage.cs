using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace CoreData
{
    public enum DeseaseType
    {
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
        List<Point> spmVals = new List<Point>();

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

            //CurveFunctions.FindGoodFit(spmVals, out BestA, out BestB, out BestC, 10, 100);
            var f = new DoubleParameterizedDelegate(AnalysisFunctions.ThreeParameterExponential);
            q = BestB;
            return true;
        }
        private bool CalcTC()
        {
            tc = 0;
            
            foreach (var pt in spmVals)
            {
                //double l = pt.X;
                //double Ke = pt.Y;
                //double Kr = BestA + BestB*Math.Exp(BestC*l);
                //tc = tc + Math.Abs((Ke - Kr)/Ke)*100;
                tc = 50;
            }
            return true;
        }
        private bool CalcISO()
        {
            iso = 0;
            for (var i=0; i < spmVals.Count - 1; ++i)
            {
                var p1 = spmVals[i];
                var p2 = spmVals[i + 1];

                var h = p2.X - p1.X;
                var S = (p1.Y + p2.Y)*h/2;
                iso = iso + S;
            }
            return true;
        }

        public bool LoadDataFromCsv(string filename)
        {
            spmVals.Clear();
            using (var reader = new StreamReader(File.OpenRead(filename)))
            {
                // болезнь
                var desStr = reader.ReadLine();
                currentDes = (DeseaseType) Convert.ToInt32(desStr);

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line?.Split(';');
                    if (values?.Length < 2)
                        continue;

                    double[] doubles;
                    bool valid = TryConvertToDoubleArr(values, out doubles);
                    if (valid)
                        spmVals.Add(new Point(doubles[0], doubles[1]));
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
