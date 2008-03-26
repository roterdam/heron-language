public class Line extends HeronObject {
  // attributes
  public Point begin;
  public Point end;
  // operations
  public Line(final double x0, final double y0, final double x1, final double y1){
    {
      begin  = new Point(x0 , y0 ) ;
      end  = new Point(x1 , y1 ) ;
      }
    }
  public double length(){
    {
      return Demo .distance (begin , end ) }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
