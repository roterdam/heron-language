public class Demo extends HeronApplication {
  // applet entry point
  public void init() {
    super.init();
    initialize();
    }
  // application entry point
  public static void main(String s[]) {
    baseMain(new Demo());
    initialize();
    theApp.dispatchNextSignal();
    }
  // attributes
  public static Painter painter;
  public static Collection<Wall> walls;
  public static Collection<Ball> balls;
  // operations
  static public void initialize(){
    {
      int x0 = 100 ;
      int y0 = 100 ;
      int x1 = 300 ;
      int y1 = 300 ;
      walls = new Collection<Wall>() ;
      balls = new Collection<Ball>() ;
      walls .add (new Wall(x0 , y0 , x1 , y0 ) ) ;
      walls .add (new Wall(x1 , y0 , x1 , y1 ) ) ;
      walls .add (new Wall(x1 , y1 , x0 , y1 ) ) ;
      walls .add (new Wall(x0 , y1 , x0 , y0 ) ) ;
      balls .add (new Ball(new Point(200 , 200 ) , new Vector(200 , 120 ) , 10 ) ) ;
      balls .add (new Ball(new Point(150 , 150 ) , new Vector(100 , 60 ) , 15 ) ) ;
      balls .add (new Ball(new Point(250 , 150 ) , new Vector(- 50 ,- 5 ) , 20 ) ) ;
      balls .add (new Ball(new Point(250 , 250 ) , new Vector(- 100 , 0 ) , 25 ) ) ;
      balls .add (new Ball(new Point(150 , 250 ) , new Vector(0 , 100 ) , 30 ) ) ;
      painter = new Painter() ;
      painter .generateNextEvent () ;
      }
    }
  static public double distance(final Point first, final Point second){
    {
      return sqrt (sqr (first .x - second .x ) + sqr (first .y - second .y ) ) ;
      }
    }
  static public double sqrt(final double x){
    {
      return Math .sqrt (x ) ;
      }
    }
  static public double sqr(final double x){
    {
      return x * x ;
      }
    }
  static public double acos(final double x){
    {
      return Math .acos (x ) ;
      }
    }
  static public double min(final double x, final double y){
    {
      return Math .min (x , y ) ;
      }
    }
  static public int floor(final double x){
    {
      return (int ) Math .floor (x ) ;
      }
    }
  static public void updateBallPositions(final double elapsedMSec){
    {
      for (Ball ball : Demo .balls )
      {
        ball .updatePosition (elapsedMSec ) ;
        }
      }
    }
  }
