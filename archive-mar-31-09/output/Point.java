public class Point extends HeronObject {
  // attributes
  public double x;
  public double y;
  // operations
  public Point(final double x, final double y){
    {
      this .x = x ;
      this .y = y ;
      }
    }
  public Vector asVector(){
    {
      return new Vector(x , y ) ;
      }
    }
  public Point translate(final Vector v){
    {
      return new Point(x + v .x , y + v .y ) ;
      }
    }
  public Vector difference(final Point pt){
    {
      return new Vector(pt .x - x , pt .y - y ) ;
      }
    }
  public double distance(final Point pt){
    {
      return difference (pt ) .length () ;
      }
    }
  public Point rotate(final Point pt, final double th){
    {
      double x1 = x * Demo .cos (th ) - y * Demo .sin (th ) ;
      double y1 = x * Demo .sin (th ) + y * Demo .cos (th ) ;
      return new Point(x1 , y1 ) ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
