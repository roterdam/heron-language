public class Line extends HeronObject {
  public static Collection<Line> instances = new Collection<Line>();
  public Line() {
    instances.add(this);
    }
  // attributes
  Point begin;
  Point end;
  // operations
  public Line(double x0, double y0, double x1, double y1){
    instances.add(this);
    {
      begin = new Point(x0 , y0 ) ;
      end = new Point(x1 , y1 ) ;
      }
    }
  public double length(){
    {
      return Demo .distance (begin , end ) ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
