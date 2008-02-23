public class Collision extends HeronObject {
  public static Collection<Collision> instances = new Collection<Collision>();
  public Collision() {
    instances.add(this);
    }
  // attributes
  int time;
  Ball target1;
  Wall target2;
  // operations
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
