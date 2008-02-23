public class Ball extends HeronObject {
  public static Collection<Ball> instances = new Collection<Ball>();
  public Ball() {
    instances.add(this);
    }
  // attributes
  Point pos;
  Vector vec;
  double radius;
  // operations
  public Ball(Point pos, Vector vec, double radius){
    instances.add(this);
    {
      this .pos = pos ;
      this .vec = vec ;
      this .radius = radius ;
      }
    }
  public Collision computeCollision(Wall wall){
    {
      double num = abs ((x2 - x1 ) * (y1 - t * y0 ) - (x1 - t * x0 ) * (y2 - y1 ) ) ;
      double den = sqrt (sqr (x2 - x1 ) + sqr (y2 - y1 ) ) ;
      double dist = num / den ;
      return null ;
      }
    }
  public Collection<Collision> computeCollisions(){
    {
      Collection < Collision > result = new Collection<Collision>() ;
      for (Ball ball : Ball .instances )
      {
        result .add (computeCollision (ball ) ) ;
        }
      for (Wall wall : Wall .instances )
      {
        result .add (computeCollision (wall ) ) ;
        }
      return result ;
      }
    }
  public void updatePosition(double elapsedMSec){
    {
      pos = pos .translate (vec .scale (elapsedMSec / 1000.0 ) ) ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
