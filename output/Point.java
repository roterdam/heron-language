public class Point extends HeronObject {
  public static Collection<Point> instances = new Collection<Point>();
  public Point() {
    instances.add(this);
    }
  // attributes
  double x;
  double y;
  // operations
  public Point(double x, double y){
    instances.add(this);
    {
      this .x = x ;
      this .y = y ;
      }
    }
  public Point translate(Vector v){
    {
      Point result = new Point(x + v .x , y + v .y ) ;
      return result ;
      }
    }
  public Vector difference(Point pt){
    {
      return new Vector(pt .x - x , pt .y - y ) ;
      }
    }
  public double distance(Point pt){
    {
      return difference (pt ) .length () ;
      }
    }
  public Vector asVector(){
    {
      return new Vector(x , y ) ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
