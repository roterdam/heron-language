public class Painter extends HeronObject {
  // attributes
  public double paintFrequency;
  public double timeToNextPaint;
  // operations
  public Painter(){
    {
      paintFrequency = 50.0 ;
      timeToNextPaint = paintFrequency ;
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
            return (x .ball != lastBall ) || (x .wall != lastWall ) ;
            }
          }
        }
       ) ;
      next = q .min (new AnonymousFunction() {
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
          timeToNextPaint = timeToNextPaint - next .timeElapsed ;
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
  // state entry procedures
  public void onCollision(HeronSignal __event__) {
    CollisionEvent col = (CollisionEvent)__event__.data;
    __state__ = 4821096;
    {
      Demo .updateBallPositions (col .timeElapsed ) ;
      col .ball .bounceOffWall (col .wall ) ;
      generateNextEvent (col .ball , col .wall ) ;
      }
    }
  public void onPaint(HeronSignal __event__) {
    PaintEvent evt = (PaintEvent)__event__.data;
    __state__ = 4827144;
    {
      timeToNextPaint = paintFrequency ;
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
      Demo .render () ;
      generateNextEvent () ;
      }
    }
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    else if ((__state__ == 0) && (signal.data instanceof CollisionEvent)) {
      onCollision(signal);
      }
    else if ((__state__ == 0) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 4821096) && (signal.data instanceof CollisionEvent)) {
      onCollision(signal);
      }
    else if ((__state__ == 4821096) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    else if ((__state__ == 4827144) && (signal.data instanceof CollisionEvent)) {
      onCollision(signal);
      }
    else if ((__state__ == 4827144) && (signal.data instanceof PaintEvent)) {
      onPaint(signal);
      }
    }
  }
