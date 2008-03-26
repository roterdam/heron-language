public class Vector extends HeronObject {
  // attributes
  public double x;
  public double y;
  // operations
  public Vector(final double x, final double y){
    {
      this .x = x ;
      this .y = y ;
      }
    }
  public Vector(final Line line){
    {
      this .x = line .begin .x - line .end .x ;
      this .y = line .begin .y - line .end .y ;
      }
    }
  public Vector add(final Vector v){
    {
      return new Vector(x + v .x , y + v .y ) }
    }
  public Vector sub(final Vector v){
    {
      return new Vector(v .x - x , v .y - y ) }
    }
  public Vector scale(final double s){
    {
      return new Vector(x * s , y * s ) }
    }
  public double dot(final Vector v){
    {
      return x * v .x + y * v .y }
    }
  public Vector normal(){
    {
      return new Vector(- y , x ) }
    }
  public Vector normalize(){
    {
      return new Vector(x / length () , y / length () ) }
    }
  public double length(){
    {
      return Demo .sqrt (Demo .sqr (x ) + Demo .sqr (y ) ) }
    }
  public double theta(final Vector v){
    {
      return Demo .acos (dot (v ) / (length () * v .length () ) ) }
    }
  public Vector tangent(){
    {
      return new Vector(y , - x ) }
    }
  public Vector proj(final Vector v){
    {
      return v .scale (dot (v ) / Demo .sqr (v .length () ) ) }
    }
  // state entry procedures
  // dispatch function
  public void onSignal(HeronSignal signal) {
    if (false) {
      }
    }
  }
