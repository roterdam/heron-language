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
      generateNextEvent (null , null ) ;
      }
    }
  public void generateNextEvent(final Ball lastBall, final Wall lastWall){
    {
      CollisionEvent next;
      Collection<CollisionEvent> q = new Collection<CollisionEvent>() ;
      q .clear () ;
      for (Ball ball : Demo .balls )
      {
        q .concat (ball .computeCollisionEvents () ) ;
        }
      q .filter (new AnonymousFunction() {
        public Object apply(Collection<Object> _args) {
          CollisionEvent x = (CollisionEvent)_args.get(0);
          {
            return (x .ball != lastBall ) || (x .wall != lastWall ) }
          }
        }
       ) ;
      next  = q .min (new AnonymousFunction() {
        public Object apply(Collection<Object> _args) {
          CollisionEvent x = (CollisionEvent)_args.get(0);
          {
            return x .timeElapsed }
          }
        }
       ) ;
      if (next != null )unhandled statement type: struct jaction_grammar::Statement
else
      unhandled statement type: struct jaction_grammar::Statement
}
    }
  public void drawShooter(){
    {
      Collection<Point> pts = Demo .shooter .getPoints () ;
      Point origin = new Point(0 , 0 ) ;
      pts  = pts .map (new AnonymousFunction() {
        public Object apply(Collection<Object> _args) {
          Point pt = (Point)_args.get(0);
          {
            return pt .rotate (origin , Demo .shooter .angle ) }
          }
        }
       ) ;
      pts  = pts .map (new AnonymousFunction() {
        public Object apply(Collection<Object> _args) {
          Point pt = (Point)_args.get(0);
          {
            return pt .translate (Demo .getBoxCenter () ) }
          }
        }
       ) ;
      int i = 1 ;
      while ((i < pts .Count ).equals(JATrue()))
      {
        Point pt1 = pts .getAt (i - 1 ) ;
        Point pt2 = pts .getAt (i ) ;
        Demo .drawLine (pt1 .x , pt1 .y , pt2 .x , pt2 .y ) ;
        ++ i ;
        }
      if (pts .Count > 1 )unhandled statement type: struct jaction_grammar::Statement
}
    }
  // state entry procedures
  public void onCollision(HeronSignal __event__) {
    CollisionEvent col = (CollisionEvent)__event__.data;
    __state__ = 4997560;
    {
      Demo .updateBallPositions (col .timeElapsed ) ;
      col .ball .bounceOffWall (col .wall ) ;
      generateNextEvent (col .ball , col .wall ) ;
      }
    }
  public void onPaint(HeronSignal __event__) {
    PaintEvent evt = (PaintEvent)__event__.data;
    __state__ = 5002840;
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
    __state__ = 5017624;
    {
      if (evt .Key == Keyboard .LeftArrow )unhandled statement type: struct jaction_grammar::Statement
else
      unhandled statement type: struct jaction_grammar::Statement
}
    }
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    else if ((__state__ == 0) && (signal.data instanceof BallWallCollisionEvent)) {
      onBallWallCollision(signal);
      }
    else if ((__state__ == 0) && (signal.data instanceof BallBallCollisionEvent)) {
      onBallBallCollsion(signal);
      }
    else if ((__state__ == 0) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 0) && (signal.data instanceof KeyboardEvent)) {
      onKeyboard(signal);
      }
    else if ((__state__ == 4997560) && (signal.data instanceof auto)) {
      initial(signal);
      }
    else if ((__state__ == 5002840) && (signal.data instanceof auto)) {
      initial(signal);
      }
    else if ((__state__ == 5017624) && (signal.data instanceof auto)) {
      initial(signal);
      }
    }
  }
