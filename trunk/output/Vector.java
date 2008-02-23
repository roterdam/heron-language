public class Vector extends HeronObject {
  public static Collection<Vector> instances = new Collection<Vector>();
  public Vector() {
    instances.add(this);
    }
  // attributes
  double x;
  double y;
  // operations
  public Vector(double x, double y){
    instances.add(this);
    {
      this .x = x ;
      this .y = y ;
      }
    }
  public Vector add(Vector v){
    {
      return new Vector(x + v .x , y + v .y ) ;
      }
    }
  public Vector scale(double s){
    {
      return new Vector(x * s , y * s ) ;
      }
    }
  public double dot(Vector v){
    {
      return x * v .x + y * v .y ;
      }
    }
  public double length(){
    {
      return Demo .sqrt (Demo .sqr (x ) + Demo .sqr (y ) ) ;
      }
    }
  public double theta(Vector v){
    {
      return Demo .acos (dot (v ) / (length () * v .length () ) ) ;
      }
    }
  public Vector tangent(){
    {
      return new Vector(y , - x ) ;
      }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
