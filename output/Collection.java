
import java.util.ArrayList;

public class Collection<T> extends ArrayList<T> {
	public T min(AnonymousFunction f) {
		Comparable ret = null;
		for (T tmp : this) {
			if (ret == null) {
				ret = null;
			}
			else {
				Comparable c = (Comparable)tmp;
				if (ret.compareTo(c) < 0)
					ret = c;
					
			}
		}			
		return (T)ret;
	}
	
	public void concat(Collection<T> coll) {
		addAll(coll);
	}
}
