public class CollisionManager extends HeronObject {
  public static Collection<CollisionManager> instances = new Collection<CollisionManager>();
  public CollisionManager() {
    instances.add(this);
    }
  // attributes
  // operations
  public Collision generateNextCollision(){
    {
      Collision next;
      Collection<Collision> q = new Collection<Collision>() ;
      q .clear () ;
      for (Ball ball : Ball .instances )
      {
        q .concat (ball .computeCollisions () ) ;
        }
      next = q .min (new AnonymousFunction() {
        public Object Apply(Collection<Object> _args) {
          Collision x = (Collision)_args.get(0);
          {
            return x .time ;
            }
          }
        }
       ) ;
      this .sendIn (col , next .time ) ;
      }
    }
  // state entry procedures
  public void onCollision(HeronSignal __event__) {
    Collision col = (Collision)__event__.data;
    __state__ = 4701288;
    {
      generateNextCollision () ;
      }
    }
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    else if ((__state__ == 0) && (signal.data instanceof Collision)) {
      painting(signal);
      }
    else if ((__state__ == 4701288) && (signal.data instanceof Collision)) {
      onCollision(signal);
      }
    }
  }
