public class Shooter extends HeronObject {
  // attributes
  public double angle;
  public double length;
  public double width;
  // operations
  public void construct(final double angle, final double length, final double width){
    {
      this .angle = angle ;
      this .length = length ;
      this .width = width ;
      }
    }
  public Collection<Point> getPoints(){
    {
      Collection<Point> result = new Collection<Point>() ;
      Point pt1 = new Point(- (width / 2 ) , length / 2 ) ;
      Point pt2 = new Point(width / 2 , length / 2 ) ;
      Point pt3 = new Point(0 , - (length / 2 ) ) ;
      result .Add (pt1 ) ;
      result .Add (pt2 ) ;
      result .Add (pt3 ) ;
      return result }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
