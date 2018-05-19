using System.Collections.Generic;
using System.Xml.Serialization;

namespace GroupingMehods
{
    public class Centre
    {
        [XmlIgnoreAttribute]
        public double activity;
        public double x;
        public double y;
        public List<Point> previousPoints;

        [XmlIgnoreAttribute]
        public List<Point> points;
        [XmlIgnoreAttribute]
        public bool IsSame
        {
            get
            {
                if (previousPoints.Count == points.Count)
                {
                    bool isSame = true;
                    for (int i = 0; i < points.Count; i++)
                    {
                        if (points[i].x != previousPoints[i].x || points[i].y != previousPoints[i].y)
                        {
                            isSame = false;
                        }
                    }
                    return isSame;
                }
                return false;
            }
        }

        public Centre(double x, double y)
        {
            this.x = x;
            this.y = y;
            this.previousPoints = new List<Point>();
            this.points = new List<Point>();
            this.activity = 1;
        }



        public Centre()
        {
            previousPoints = new List<Point>();
            points = new List<Point>();
            activity = 1;
        }



        public void SetNewCordinates()
        {
            double avgX = 0;
            double avgY = 0;
            if (points.Count != 0)
            {
                points.ForEach(p => avgX += p.x);
                points.ForEach(p => avgY += p.y);
                avgX /= points.Count;
                avgY /= points.Count;
                x = avgX;
                y = avgY;
            }
        }



        public void ResetPoints()
        {
            previousPoints.Clear();
            previousPoints.TrimExcess();
            for (int i = 0; i < points.Count; i++)
            {
                previousPoints.Add(new Point(points[i].x, points[i].y));
            }
            points.Clear();
            points.TrimExcess();
        }


    }
}
