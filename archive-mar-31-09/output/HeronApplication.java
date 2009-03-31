import java.awt.*;
import java.awt.event.*;
import java.awt.geom.*;
import java.util.ArrayList;
import javax.swing.*;

public class HeronApplication extends JApplet implements KeyListener
{
	public class Dispatcher extends Thread
	{
		public synchronized void run()
		{
			while (true) {
				try { 
					// TODO: compute exactly the right amount of time to wait. 
					wait(10);
					HeronApplication.theApp.dispatchNextSignal();
				}
				catch (InterruptedException e) {					
				}
			}
		}
	}

	static HeronObject heronKeyListener;
	
	private static final long serialVersionUID = -4394966026114834504L;
	final static int maxCharHeight = 15;
	final static int minFontSize = 6;

	public static ArrayList<Shape> shapes = new ArrayList<Shape>();
	
	final static Color bg = Color.white;
	final static Color fg = Color.black;
	final static Color red = Color.red;
	final static Color white = Color.white;

	final static BasicStroke stroke = new BasicStroke(2.0f);
	final static BasicStroke wideStroke = new BasicStroke(8.0f);

	final static float dash1[] = { 10.0f };
	final static BasicStroke dashed = new BasicStroke(1.0f,
			BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER, 10.0f, dash1, 0.0f);
	Dimension totalSize;
	FontMetrics fontMetrics;
	
	public static JFrame frame = null;
	public static HeronApplication theApp = null;
	public Dispatcher dispatcher;

	public void init() {
		theApp = this;
		setBackground(bg);
		setForeground(fg);	
		dispatcher = new Dispatcher();
		dispatcher.start();		
	}
		
	synchronized public void paint(Graphics g) {
		Graphics2D g2 = (Graphics2D)g;
		Dimension d = getSize();
			
		Image image = GraphicsEnvironment.getLocalGraphicsEnvironment().getDefaultScreenDevice().getDefaultConfiguration().createCompatibleImage(d.width, d.height);
		Graphics2D imgG = (Graphics2D)image.getGraphics();

		imgG.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
		imgG.setBackground(bg);
		imgG.clearRect(0, 0, d.width, d.height);
		imgG.setPaint(fg);

		for (int i=0; i < shapes.size(); ++i) {
			imgG.draw(shapes.get(i));
		}
		
		g2.drawImage(image, 0, 0, this);		
	}
	
	public static void drawChord(double x, double y, double rad, double start, double finish) {
		drawOpenArc(x - rad, y - rad, 2 * rad, 2 * rad, start, finish);
	}
	
	public static void drawOpenArc(double x, double y, double width, double height, double start, double finish) {
		shapes.add(new Arc2D.Double(x, y, width, height, radToDeg(start), radToDeg(finish), Arc2D.OPEN));
	}
	
	public static void drawLine(double x0, double y0, double x1, double y1) {
		shapes.add(new Line2D.Double(x0, y0, x1, y1));
	}
	
	public static void drawCircle(double x, double y, double rad) {
		drawChord(x, y, rad, 0, Math.PI * 2);
	}
	
	public static double degToRad(int deg) {
		return (Math.PI * 2 * deg) / 360;
	}
	
	public static double radToDeg(double rad) {
		return (360 * rad) / (2 * Math.PI);
	}
	
	public static double pi = Math.PI;
	
	public static double sqr(double x) {
		return x * x;
	}
	
	public static double acos(double x) {
		return Math.acos(x);
	}
	
	public static double sqrt(double x) {
		return Math.sqrt(x);
	}
	
	public synchronized static void clear() {
		shapes.clear();
	}
	
	public static HeronDateTime now() {
		return new HeronDateTime();
	}
	
	public synchronized static void render() {
		theApp.repaint();
		//Graphics myGraphics = getPaintGraphics();
	}	

	public static void baseMain(HeronApplication app) {
		theApp = app;
		frame = new JFrame("Heron Demo");
		frame.addWindowListener(new WindowAdapter() {
			public void windowClosing(WindowEvent e) {
				System.exit(0);
			}
		});		
		frame.addKeyListener(theApp);
		frame.getContentPane().add("Center", theApp);
		frame.pack();
		frame.setSize(new Dimension(550, 500));
		frame.setVisible(true);
		render();		
		theApp.init();
	}
	
	public static void error(String s) {
		System.console().printf(s);
	}

	//================================================================================
	// Signal/Event handling stuff 
	
	public ArrayList<HeronSignal> signals = new ArrayList<HeronSignal>();
	
	public void sendIn(HeronObject target, Object data, int msec) {
		sendAt(target, data, (new HeronDateTime()).addMSec(msec));
	}
	public void sendAt(HeronObject target, Object data, HeronDateTime time) {
		HeronSignal sig = new HeronSignal();
		sig.data = data;
		sig.scheduledTime = time;
		sig.target = target;
		signals.add(sig);
	}
	public HeronSignal getNextSignal()
	{	
		// TODO: Improve by using a priority queue
		HeronSignal ret = null;
		HeronDateTime now = new HeronDateTime();
		for (int i=0; i < signals.size(); ++i) {
			HeronSignal tmp = signals.get(i);
			if (tmp.scheduledTime.before(now))
			{
				if (ret == null)
					ret = signals.get(i);
				else if (tmp.scheduledTime.before(ret.scheduledTime)) 					
					ret = signals.get(i);					
			}
		}
		if (ret != null)
			signals.remove(ret);
		return ret;
	}
	
	synchronized public void dispatchNextSignal() {		
		HeronSignal sig = getNextSignal();
		if (sig != null) {
			sig.target.onSignal(sig);
		}
	}
	
    public void keyPressed(KeyEvent e) {
		if (heronKeyListener != null) {
		  HeronSignal sig = new HeronSignal();
		  sig.data = e;
		  sig.scheduledTime = new HeronDateTime();
		  sig.target = heronKeyListener;			
		  heronKeyListener.onSignal(sig);
		}
    }
    
    public static void registerKeyListener(HeronObject o) {
       heronKeyListener = o;
    }
    
	public void keyReleased(KeyEvent e) {
		// do nothing
	}
	
    public void keyTyped(KeyEvent e) {
		// do nothing
    }
	
}
