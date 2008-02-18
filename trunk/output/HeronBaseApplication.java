import java.awt.*;
import java.awt.event.*;
import java.awt.geom.*;
import java.util.ArrayList;

import javax.swing.*;

public class HeronBaseApplication extends JApplet 
{
	private static final long serialVersionUID = -4394966026114834504L;
	final static int maxCharHeight = 15;
	final static int minFontSize = 6;

	final static ArrayList<Shape> shapes = new ArrayList<Shape>();
	
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

	public void init() {
		// Initialize drawing colors
		setBackground(bg);
		setForeground(fg);
	}

	public void paint(Graphics g) {
		Graphics2D g2 = (Graphics2D) g;
	
		g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING,
				RenderingHints.VALUE_ANTIALIAS_ON);
		Dimension d = getSize();

		Color fg3D = Color.lightGray;

		g2.setPaint(fg3D);
		g2.draw3DRect(0, 0, d.width - 1, d.height - 1, true);
		g2.draw3DRect(3, 3, d.width - 7, d.height - 7, false);
		g2.setPaint(fg);

		for (int i=0; i < shapes.size(); ++i) {
			g2.draw(shapes.get(i));
		}	
	}
	
	public static void drawChord(double x, double y, double rad, double start, double finish) {
		drawOpenArc(x - rad, y - rad, x + rad, y + rad, start, finish);
	}
	
	public static void drawOpenArc(double x, double y, double width, double height, double start, double finish) {
		shapes.add(new Arc2D.Double(x, y, width, height, radToDeg(start), radToDeg(finish), Arc2D.OPEN));
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
	
	public static void clear() {
		shapes.clear();
	}

	public static void baseMain(JApplet applet) {
		JFrame f = new JFrame("ShapesDemo2D");
		f.addWindowListener(new WindowAdapter() {
			public void windowClosing(WindowEvent e) {
				System.exit(0);
			}
		});		
		f.getContentPane().add("Center", applet);
		applet.init();
		f.pack();
		f.setSize(new Dimension(550, 500));
		f.setVisible(true);
	}
}
