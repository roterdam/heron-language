public class Ball extends HeronObject {
  // attributes
  public Point pos;
  public Vector vec;
  public double radius;
  // operations
  public Ball(final Point pos, final Vector vec, final double radius){
    {
      this .pos = pos ;
      this .vec = vec ;
      this .radius = radius ;
      }
    }
  public CollisionEvent computeCollisionEvent(final Wall wall){
    {
      double x0 = pos .x ;
      double y0 = pos .y ;
      double xt = vec .x ;
      double yt = vec .y ;
      double x1 = wall .line .begin .x ;
      double y1 = wall .line .begin .y ;
      double x2 = wall .line .end .x ;
      double y2 = wall .line .end .y ;
      double den = Demo .sqrt (Demo .sqr (x2 - x1 ) + Demo .sqr (y2 - y1 ) ) ;
      double dx = x2 - x1 ;
      double dy = y2 - y1 ;
      double t1 = ((radius * den ) - (dx * y1 ) + (dx * y0 ) + (dy * x1 ) - (dy * x0 ) ) / (- dx * yt + dy * xt ) ;
      double t2 = (- (radius * den ) - (dx * y1 ) + (dx * y0 ) + (dy * x1 ) - (dy * x0 ) ) / (- dx * yt + dy * xt ) ;
      double t = Demo .min (t1 , t2 ) ;
      if (t1 <= 0.0 ){
        if (t2 <= 0.0 ){
          return null ;
          }
        else
        {
          t = t2 ;
          }
        }
      else
      {
        if (t2 < 0 ){
          t = t1 ;
          }
        else
        {
          t = Demo .min (t1 , t2 ) ;
          }
        }
      CollisionEvent ret = new CollisionEvent(Demo .floor (t * 1000 ) , this , wall ) ;
      return ret ;
      }
    }
  public Collection<CollisionEvent> computeCollisionEvents(){
    {
      Collection < CollisionEvent > result = new Collection<CollisionEvent>() ;
      for (Wall wall : Demo .walls )
      {
        CollisionEvent collision = computeCollisionEvent (wall ) ;
        if (collision != null ){
          result .add (collision ) ;
          }
        }
      return result ;
      }
    }
  public void bounceOffWall(final Wall wall){
    {
      Vector w = new Vector(wall .line ) ;
      Vector n = w .normal () ;
      Vector pn = vec .proj (n ) ;
      Vector pw = vec .proj (w ) ;
      Vector r = pn .scale (- 1.0 ) .add (pw ) ;
      vec .x = r .x ;
      vec .y = r .y ;
      }
    }
  public void updatePosition(final double elapsedMSec){
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
