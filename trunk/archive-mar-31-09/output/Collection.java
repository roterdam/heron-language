
import java.util.ArrayList;

public class Collection<T> extends ArrayList<T> {
	public T min(AnonymousFunction f) {
		Comparable c = null;
		T ret = null;
		Collection<Object> args = new Collection<Object>();
		args.add(null);		
		for (T tmp : this) {
			args.set(0, tmp);
			Comparable cTmp = (Comparable)f.apply(args);
			if (ret == null) {
				ret = tmp;
				c = cTmp;
			}
			else {
				if (cTmp.compareTo(c) < 0) {
					c = cTmp;
					ret = tmp;
				}
			}
		}			
		return ret;
	}
	
	Collection<T> filter(AnonymousFunction f) {
		Collection<T> ret = new Collection<T>();
		Collection<Object> args = new Collection<Object>();
		args.add(null);		
		for (int i=size() - 1; i >= 0; --i) {
			args.set(0, get(i));
			boolean bTmp = (Boolean)f.apply(args);
			if (bTmp) {
				ret.add(get(i));
			}
		}			
		return ret;
	}
	
	Collection<T> map(AnonymousFunction f) {
		Collection<T> ret = new Collection<T>();
		Collection<Object> args = new Collection<Object>();
		args.add(null);		
		for (int i=size() - 1; i >= 0; --i) {
			args.set(0, get(i));
			ret.add((T)f.apply(args));
		}			
		return ret;
	}
		
	public void concat(Collection<T> coll) {
		addAll(coll);
	}
}
