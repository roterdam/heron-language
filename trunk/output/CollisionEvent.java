public class CollisionEvent extends HeronObject {
  // attributes
  public double timeElapsed;
  public Ball ball;
  public Wall wall;
  // operations
  public CollisionEvent(final double timeElapsed, final Ball ball, final Wall wall){
    {
      this .timeElapsed = timeElapsed ;
      this .ball = ball ;
      this .wall = wall ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
