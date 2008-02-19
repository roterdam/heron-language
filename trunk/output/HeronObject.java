public class HeronObject {
	public void send(Object o) {
		sendIn(o, 0);
	}
	public void sendIn(Object o, int msec) {
		HeronApplication.theApp.sendIn(this, o, msec);
	}
	public void onSignal(HeronSignal signal) {		
		// Do nothing by default
		// active objects should overload this function 	
	}
	public void unrecognizedState() throws Exception {
		throw new Exception("unrecognized state");
	}

	protected int __state__ = 0;
}
