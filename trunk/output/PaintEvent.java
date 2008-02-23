public class PaintEvent extends HeronObject {
  public static Collection<PaintEvent> instances = new Collection<PaintEvent>();
  public PaintEvent() {
    instances.add(this);
    }
  // attributes
  int frequency;
  // operations
  public PaintEvent(int freq){
    instances.add(this);
    {
      frequency = freq ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
