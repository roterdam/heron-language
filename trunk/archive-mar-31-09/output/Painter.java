public class Painter extends HeronObject {
  // attributes
  public double paintFrequency;
  public double timeToNextPaint;
  // operations
  public Painter(){
    {
      paintFrequency  = 50.0 ;
      timeToNextPaint  = paintFrequency ;
      }
    }
  public void generateNextEvent(){
    {
      CollisionEvent next;
      Collection<CollisionEvent> q = new Collection<CollisionEvent>() ;
      q .clear () ;
      for (Ball ball : Demo .balls )
      {
        q .concat (ball .computeCollisionEvents () ) ;
        }
      next  = q .min (new AnonymousFunction() {
        public Object apply(Collection<Object> _args) {
          CollisionEvent x = (CollisionEvent)_args.get(0);
          {
            return x .timeElapsed ;
            }
          }
        }
       ) ;
      if (next != null ){
        if (next .timeElapsed < timeToNextPaint ){
          timeToNextPaint  = timeToNextPaint - next .timeElapsed ;
          this .sendIn (next , (int ) next .timeElapsed ) ;
          }
        else
        {
          this .sendIn (new PaintEvent(timeToNextPaint ) , (int ) timeToNextPaint ) ;
          }
        }
      else
      {
        Demo .error ("could not compute next collision" ) ;
        }
      }
    }
  public void drawShooter(){
    {
      Collection<Point> pts = Demo .shooter .getPoints () ;
      pts  = pts .map (new AnonymousFunction() {
        public Object apply(Collection<Object> _args) {
          Point pt = (Point)_args.get(0);
          {
            Point origin = new Point(0 , 0 ) ;
            return pt .rotate (origin , Demo .shooter .angle ) ;
            }
          }
        }
       ) ;
      pts  = pts .map (new AnonymousFunction() {
        public Object apply(Collection<Object> _args) {
          Point pt = (Point)_args.get(0);
          {
            return pt .translate (Demo .boxCenter () .asVector () ) ;
            }
          }
        }
       ) ;
      int i = 1 ;
      while (i < pts .size () )
      {
        Point pt1 = pts .get (i - 1 ) ;
        Point pt2 = pts .get (i ) ;
        Demo .drawLine (pt1 .x , pt1 .y , pt2 .x , pt2 .y ) ;
        ++ i ;
        }
      if (pts .size () > 1 ){
        Point pt1 = pts .get (0 ) ;
        Point pt2 = pts .get (pts .size () - 1 ) ;
        Demo .drawLine (pt1 .x , pt1 .y , pt2 .x , pt2 .y ) ;
        }
      }
    }
  // state entry procedures
  public void onBallCollision(HeronSignal __event__) {
    WallCollisionEvent col = (WallCollisionEvent)__event__.data;
    __state__ = 5006512;
    {
      }
    }
  public void onWallCollision(HeronSignal __event__) {
    WallCollisionEvent col = (WallCollisionEvent)__event__.data;
    __state__ = 5009488;
    {
      Demo .updateBallPositions (col .timeElapsed ) ;
      col .ball .bounceOffWall (col .wall ) ;
      generateNextEvent () ;
      }
    }
  public void onPaint(HeronSignal __event__) {
    PaintEvent evt = (PaintEvent)__event__.data;
    __state__ = 5015248;
    {
      timeToNextPaint  = paintFrequency ;
      Demo .clear () ;
      Demo .updateBallPositions (evt .timeElapsed ) ;
      for (Wall wall : Demo .walls )
      {
        Demo .drawLine (wall .line .begin .x , wall .line .begin .y , wall .line .end .x , wall .line .end .y ) ;
        }
      for (Ball ball : Demo .balls )
      {
        Demo .drawCircle (ball .pos .x , ball .pos .y , ball .radius ) ;
        }
      drawShooter () ;
      Demo .render () ;
      generateNextEvent () ;
      }
    }
  public void onKey(HeronSignal __event__) {
    KeyEvent evt = (KeyEvent)__event__.data;
    __state__ = 5031472;
    {
      if (evt .key == Demo .leftArrow ){
        Demo .shooter .turnLeft () ;
        }
      else
      if (evt .key == Demo .rightArrow ){
        Demo .shooter .turnRight () ;
        }
      else
      if (evt .key == Demo .space ){
        Demo .shooter .shoot () ;
        }
      }
    }
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    else if ((__state__ == 0) && (signal.data instanceof WallCollisionEvent)) {
      onWallCollision(signal);
      }
    else if ((__state__ == 0) && (signal.data instanceof BallCollisionEvent)) {
      onBallCollision(signal);
      }
    else if ((__state__ == 0) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 0) && (signal.data instanceof KeyEvent)) {
      onKey(signal);
      }
    else if ((__state__ == 5006512) && (signal.data instanceof WallCollisionEvent)) {
      onWallCollision(signal);
      }
    else if ((__state__ == 5006512) && (signal.data instanceof BallCollisionEvent)) {
      onBallCollision(signal);
      }
    else if ((__state__ == 5006512) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 5006512) && (signal.data instanceof KeyEvent)) {
      onKey(signal);
      }
    else if ((__state__ == 5009488) && (signal.data instanceof WallCollisionEvent)) {
      onWallCollision(signal);
      }
    else if ((__state__ == 5009488) && (signal.data instanceof BallCollisionEvent)) {
      onBallCollision(signal);
      }
    else if ((__state__ == 5009488) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 5009488) && (signal.data instanceof KeyEvent)) {
      onKey(signal);
      }
    else if ((__state__ == 5015248) && (signal.data instanceof WallCollisionEvent)) {
      onWallCollision(signal);
      }
    else if ((__state__ == 5015248) && (signal.data instanceof BallCollisionEvent)) {
      onBallCollision(signal);
      }
    else if ((__state__ == 5015248) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 5015248) && (signal.data instanceof KeyEvent)) {
      onKey(signal);
      }
    else if ((__state__ == 5031472) && (signal.data instanceof WallCollisionEvent)) {
      onWallCollision(signal);
      }
    else if ((__state__ == 5031472) && (signal.data instanceof BallCollisionEvent)) {
      onBallCollision(signal);
      }
    else if ((__state__ == 5031472) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 5031472) && (signal.data instanceof KeyEvent)) {
      onKey(signal);
      }
    }
  }
