public class Demo extends HeronApplication {
  // main entry point
  public static void main(String s[]) {
    baseMain(new Demo());
    initialize();
    theApp.dispatchNextSignal();
    }
  // operations
  static public void initialize(){
    {
      int x0 = 5 ;
      int y0 = 5 ;
      int x1 = 195 ;
      int y1 = 195 ;
      new Wall(x0 , y0 , x1 , y0 ) ;
      new Wall(x1 , y0 , x1 , y1 ) ;
      new Wall(x1 , y1 , x0 , y1 ) ;
      new Wall(x0 , y1 , x0 , y0 ) ;
      new Ball(new Point(100 , 100 ) , new Vector(20 , 10 ) , 10 ) ;
      Painter painter = new Painter() ;
      painter .send (new PaintEvent(100 ) ) ;
      }
    }
  static public double distance(Point first, Point second){
    {
      return sqrt (sqr (first .x - second .x ) + sqr (first .y - second .y ) ) ;
      }
    }
  static public double sqrt(double x){
    {
      return Math .sqrt (x ) ;
      }
    }
  static public double sqr(double x){
    {
      return x * x ;
      }
    }
  static public double acos(double x){
    {
      return Math .acos (x ) ;
      }
    }
  }
