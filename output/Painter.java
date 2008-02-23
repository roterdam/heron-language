public class Painter extends HeronObject {
  public static Collection<Painter> instances = new Collection<Painter>();
  public Painter() {
    instances.add(this);
    }
  // attributes
  // operations
  // state entry procedures
  public void painting(HeronSignal __event__) {
    PaintEvent evt = (PaintEvent)__event__.data;
    __state__ = 4707336;
    {
      Demo .clear () ;
      for (Wall wall : Wall .instances )
      {
        Demo .drawLine (wall .line .begin .x , wall .line .begin .y , wall .line .end .x , wall .line .end .y ) ;
        }
      for (Ball ball : Ball .instances )
      {
        ball .updatePosition (evt .frequency ) ;
        Demo .drawCircle (ball .pos .x , ball .pos .y , ball .radius ) ;
        }
      Demo .render () ;
      this .sendIn (evt , evt .frequency ) ;
      }
    }
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    else if ((__state__ == 0) && (signal.data instanceof PaintEvent)) {
      painting(signal);
      }
    else if ((__state__ == 4707336) && (signal.data instanceof PaintEvent)) {
      painting(signal);
      }
    }
  }
