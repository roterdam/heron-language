public class Wall extends HeronObject {
  public static Collection<Wall> instances = new Collection<Wall>();
  public Wall() {
    instances.add(this);
    }
  // attributes
  Line line;
  // operations
  public Wall(double x0, double y0, double x1, double y1){
    instances.add(this);
    {
      line = new Line(x0 , y0 , x1 , y1 ) ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
