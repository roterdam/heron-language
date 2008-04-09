public class BallCollisionEvent extends CollisionEvent{
  // attributes
  public Ball ball1;
  public Ball ball2;
  // operations
  public BallCollisionEvent(final double timeElapsed, final Ball ball1, final Ball ball2){
    {
      this .timeElapsed = timeElapsed ;
      this .ball1 = ball1 ;
      this .ball2 = ball2 ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
