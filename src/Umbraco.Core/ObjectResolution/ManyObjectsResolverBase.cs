using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Umbraco.Core.ObjectResolution
{
	/// <summary>
	/// The base class for all many-objects resolvers.
	/// </summary>
	/// <typeparam name="TResolver">The type of the concrete resolver class.</typeparam>
	/// <typeparam name="TResolved">The type of the resolved objects.</typeparam>
	public abstract class ManyObjectsResolverBase<TResolver, TResolved> : ResolverBase<TResolver>
		where TResolved : class 
		where TResolver : class
	{
		private IEnumerable<TResolved> _applicationInstances = null;
		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
		private readonly List<Type> _instanceTypes = new List<Type>(); 

		private int _defaultPluginWeight = 10;

		#region Constructors
			
		/// <summary>
		/// Initializes a new instance of the <see cref="ManyObjectsResolverBase{TResolver, TResolved}"/> class with an empty list of objects.
		/// </summary>
		/// <param name="scope">The lifetime scope of instantiated objects, default is per Application.</param>
		/// <remarks>If <paramref name="scope"/> is per HttpRequest then there must be a current HttpContext.</remarks>
		/// <exception cref="InvalidOperationException"><paramref name="scope"/> is per HttpRequest but the current HttpContext is null.</exception>
		protected ManyObjectsResolverBase(ObjectLifetimeScope scope = ObjectLifetimeScope.Application)
		{
			CanResolveBeforeFrozen = false;
			if (scope == ObjectLifetimeScope.HttpRequest)
			{
				if (HttpContext.Current == null)
					throw new InvalidOperationException("Use alternative constructor accepting a HttpContextBase object in order to set the lifetime scope to HttpRequest when HttpContext.Current is null");		

				CurrentHttpContext = new HttpContextWrapper(HttpContext.Current);
			}

			LifetimeScope = scope;
			_instanceTypes = new List<Type>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManyObjectsResolverBase{TResolver, TResolved}"/> class with an empty list of objects,
		/// with creation of objects based on an HttpRequest lifetime scope.
		/// </summary>
		/// <param name="httpContext">The HttpContextBase corresponding to the HttpRequest.</param>
		/// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <c>null</c>.</exception>
		protected ManyObjectsResolverBase(HttpContextBase httpContext)
		{
			CanResolveBeforeFrozen = false;
			if (httpContext == null)
				throw new ArgumentNullException("httpContext");
			LifetimeScope = ObjectLifetimeScope.HttpRequest;
			CurrentHttpContext = httpContext;
			_instanceTypes = new List<Type>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManyObjectsResolverBase{TResolver, TResolved}"/> class with an initial list of object types.
		/// </summary>
		/// <param name="value">The list of object types.</param>
		/// <param name="scope">The lifetime scope of instantiated objects, default is per Application.</param>
		/// <remarks>If <paramref name="scope"/> is per HttpRequest then there must be a current HttpContext.</remarks>
		/// <exception cref="InvalidOperationException"><paramref name="scope"/> is per HttpRequest but the current HttpContext is null.</exception>
		protected ManyObjectsResolverBase(IEnumerable<Type> value, ObjectLifetimeScope scope = ObjectLifetimeScope.Application)
			: this(scope)
		{
			_instanceTypes = value.ToList();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ManyObjectsResolverBase{TResolver, TResolved}"/> class with an initial list of objects,
		/// with creation of objects based on an HttpRequest lifetime scope.
		/// </summary>
		/// <param name="httpContext">The HttpContextBase corresponding to the HttpRequest.</param>
		/// <param name="value">The list of object types.</param>
		/// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <c>null</c>.</exception>
		protected ManyObjectsResolverBase(HttpContextBase httpContext, IEnumerable<Type> value)
			: this(httpContext)
		{
			_instanceTypes = value.ToList();
		} 
		#endregion

		/// <summary>
		/// Gets or sets a value indicating whether the resolver can resolve objects before resolution is frozen.
		/// </summary>
		/// <remarks>This is false by default and is used for some special internal resolvers.</remarks>
		internal bool CanResolveBeforeFrozen { get; set; }

		/// <summary>
		/// Gets the list of types to create instances from.
		/// </summary>
		protected virtual IEnumerable<Type> InstanceTypes
		{
			get { return _instanceTypes; }
		}

		/// <summary>
		/// Gets or sets the <see cref="HttpContextBase"/> used to initialize this object, if any.
		/// </summary>
		/// <remarks>If not null, then <c>LifetimeScope</c> will be <c>ObjectLifetimeScope.HttpRequest</c>.</remarks>
		protected HttpContextBase CurrentHttpContext { get; private set; }

		/// <summary>
		/// Gets or sets the lifetime scope of resolved objects.
		/// </summary>
		protected ObjectLifetimeScope LifetimeScope { get; private set; }

		/// <summary>
		/// Gets the resolved object instances, sorted by weight.
		/// </summary>
		/// <returns>The sorted resolved object instances.</returns>
		/// <remarks>
		/// <para>The order is based upon the <c>WeightedPluginAttribute</c> and <c>DefaultPluginWeight</c>.</para>
		/// <para>Weights are sorted ascendingly (lowest weights come first).</para>
		/// </remarks>
		protected IEnumerable<TResolved> GetSortedValues()
		{
			var values = Values.ToList();

			// FIXME - so we're re-sorting each time?

			values.Sort((f1, f2) => GetObjectWeight(f1).CompareTo(GetObjectWeight(f2)));
			return values;
		}

		/// <summary>
		/// Gets or sets the default type weight.
		/// </summary>
		/// <remarks>Determines the weight of types that do not have a <c>WeightedPluginAttribute</c> set on 
		/// them, when calling <c>GetSortedValues</c>.</remarks>
		protected virtual int DefaultPluginWeight
		{
			get { return _defaultPluginWeight; }
			set { _defaultPluginWeight = value; }
		}

		int GetObjectWeight(object o)
		{
			var type = o.GetType();
			var attr = type.GetCustomAttribute<WeightedPluginAttribute>(true);
			return attr == null ? DefaultPluginWeight : attr.Weight;
		}

		/// <summary>
		/// Gets the resolved object instances.
		/// </summary>
		/// <exception cref="InvalidOperationException"><c>CanResolveBeforeFrozen</c> is false, and resolution is not frozen.</exception>
		protected IEnumerable<TResolved> Values
		{
			get
			{				
				// cannot return values unless resolution is frozen, or we can
				if (!CanResolveBeforeFrozen && !Resolution.IsFrozen)
					throw new InvalidOperationException("Values cannot be returned until resolution is frozen");

				// note: we apply .ToArray() to the output of CreateInstance() because that is an IEnumerable that
				// comes from the PluginManager we want to be _sure_ that it's not a Linq of some sort, but the
				// instances have actually been instanciated when we return.

				switch (LifetimeScope)
				{
					case ObjectLifetimeScope.HttpRequest:
						// create new instances per HttpContext
						var key = this.GetType().FullName; // use full type name as key
						using (var l = new UpgradeableReadLock(_lock))
						{
							// create if not already there
							if (CurrentHttpContext.Items[key] == null)
							{
								l.UpgradeToWriteLock();
								CurrentHttpContext.Items[key] = CreateInstances().ToArray();
							}
							return (List<TResolved>)CurrentHttpContext.Items[key];
						}

					case ObjectLifetimeScope.Application:
						// create new instances per application
						using(var l = new UpgradeableReadLock(_lock))
						{
							// create if not already there
							if (_applicationInstances == null)
							{
								l.UpgradeToWriteLock();
								_applicationInstances = CreateInstances().ToArray();
							}
							return _applicationInstances;
						}

					case ObjectLifetimeScope.Transient:
					default:
						// create new instances each time
						return CreateInstances().ToArray();
				}				
			}
		}

		/// <summary>
		/// Creates the object instances for the types contained in the types collection.
		/// </summary>
		/// <returns>A list of objects of type <typeparamref name="TResolved"/>.</returns>
		protected virtual IEnumerable<TResolved> CreateInstances()
		{
			return PluginManager.Current.CreateInstances<TResolved>(InstanceTypes);
		}

		/// <summary>
		/// Ensures that a type is a valid type for the resolver.
		/// </summary>
		/// <param name="value">The type to test.</param>
		/// <exception cref="InvalidOperationException"> the type is not a valid type for the resolver.</exception>
		protected void EnsureCorrectType(Type value)
		{
			if (!TypeHelper.IsTypeAssignableFrom<TResolved>(value))
				throw new InvalidOperationException(string.Format(
					"Type {0} is not an acceptable type for resolver {1}.", value.FullName, this.GetType().FullName));
		}

		#region Types collection manipulation

		/// <summary>
		/// Removes a type.
		/// </summary>
		/// <param name="value">The type to remove.</param>
		/// <exception cref="InvalidOperationException">the resolver does not support removing types, or 
		/// the type is not a valid type for the resolver.</exception>
		public virtual void RemoveType(Type value)
		{
			EnsureRemoveSupport();
			EnsureResolutionNotFrozen();

			using (var l = new UpgradeableReadLock(_lock))
			{
				EnsureCorrectType(value);

				l.UpgradeToWriteLock();
				_instanceTypes.Remove(value);
			}
		}

		/// <summary>
		/// Removes a type.
		/// </summary>
		/// <typeparam name="T">The type to remove.</typeparam>
		/// <exception cref="InvalidOperationException">the resolver does not support removing types, or 
		/// the type is not a valid type for the resolver.</exception>
		public void RemoveType<T>()
		{
			RemoveType(typeof(T));
		}

		/// <summary>
		/// Adds types.
		/// </summary>
		/// <param name="types">The types to add.</param>
		/// <remarks>The types are appended at the end of the list.</remarks>
		/// <exception cref="InvalidOperationException">the resolver does not support adding types, or 
		/// a type is not a valid type for the resolver, or a type is already in the collection of types.</exception>
		protected void AddTypes(IEnumerable<Type> types)
		{
			EnsureAddSupport();
			EnsureResolutionNotFrozen();

			using (new WriteLock(_lock))
			{
				foreach(var t in types)
				{
					EnsureCorrectType(t);
					if (InstanceTypes.Contains(t))
					{
						throw new InvalidOperationException(string.Format(
							"Type {0} is already in the collection of types.", t.FullName));
					}
					_instanceTypes.Add(t);	
				}				
			}
		}

		/// <summary>
		/// Adds a type.
		/// </summary>
		/// <param name="value">The type to add.</param>
		/// <remarks>The type is appended at the end of the list.</remarks>
		/// <exception cref="InvalidOperationException">the resolver does not support adding types, or 
		/// the type is not a valid type for the resolver, or the type is already in the collection of types.</exception>
		public virtual void AddType(Type value)
		{
			EnsureAddSupport();
			EnsureResolutionNotFrozen();

			using (var l = new UpgradeableReadLock(_lock))
			{
				EnsureCorrectType(value);
				if (InstanceTypes.Contains(value))
				{
					throw new InvalidOperationException(string.Format(
						"Type {0} is already in the collection of types.", value.FullName));
				}

				l.UpgradeToWriteLock();
				_instanceTypes.Add(value);
			}
		}

		/// <summary>
		/// Adds a type.
		/// </summary>
		/// <typeparam name="T">The type to add.</typeparam>
		/// <remarks>The type is appended at the end of the list.</remarks>
		/// <exception cref="InvalidOperationException">the resolver does not support adding types, or 
		/// the type is not a valid type for the resolver, or the type is already in the collection of types.</exception>
		public void AddType<T>()
		{
			AddType(typeof(T));
		}

		/// <summary>
		/// Clears the list of types.
		/// </summary>
		/// <exception cref="InvalidOperationException">the resolver does not support clearing types.</exception>
		public virtual void Clear()
		{
			EnsureClearSupport();
			EnsureResolutionNotFrozen();

			using (new WriteLock(_lock))
			{
				_instanceTypes.Clear();
			}
		}

		/// <summary>
		/// Inserts a type at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which the type should be inserted.</param>
		/// <param name="value">The type to insert.</param>
		/// <exception cref="InvalidOperationException">the resolver does not support inserting types, or 
		/// the type is not a valid type for the resolver, or the type is already in the collection of types.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
		public virtual void InsertType(int index, Type value)
		{			
			EnsureInsertSupport();
			EnsureResolutionNotFrozen();

			using (var l = new UpgradeableReadLock(_lock))
			{
				EnsureCorrectType(value);
				if (InstanceTypes.Contains(value))
				{
					throw new InvalidOperationException(string.Format(
						"Type {0} is already in the collection of types.", value.FullName));
				}

				l.UpgradeToWriteLock();
				_instanceTypes.Insert(index, value);
			}
		}

		/// <summary>
		/// Inserts a type at the specified index.
		/// </summary>
		/// <typeparam name="T">The type to insert.</typeparam>
		/// <param name="index">The zero-based index at which the type should be inserted.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
		public void InsertType<T>(int index)
		{
			InsertType(index, typeof(T));
		}

		/// Inserts a type before a specified, already existing type.
		/// </summary>
		/// <param name="existingType">The existing type before which to insert.</param>
		/// <param name="value">The type to insert.</param>
		/// <exception cref="InvalidOperationException">the resolver does not support inserting types, or 
		/// one of the types is not a valid type for the resolver, or the existing type is not in the collection,
		/// or the new type is already in the collection of types.</exception>
		public virtual void InsertTypeBefore(Type existingType, Type value)
		{
			EnsureInsertSupport();
			EnsureResolutionNotFrozen();

			using (var l = new UpgradeableReadLock(_lock))
			{
				EnsureCorrectType(existingType);
				EnsureCorrectType(value);
				if (!InstanceTypes.Contains(existingType))
				{
					throw new InvalidOperationException(string.Format(
						"Type {0} is not in the collection of types.", existingType.FullName));
				}
				if (InstanceTypes.Contains(value))
				{
					throw new InvalidOperationException(string.Format(
						"Type {0} is already in the collection of types.", value.FullName));
				}
				int index = InstanceTypes.IndexOf(existingType);

				l.UpgradeToWriteLock();
				_instanceTypes.Insert(index, value);
			}
		}

		/// <summary>
		/// Inserts a type before a specified, already existing type.
		/// </summary>
		/// <typeparam name="Texisting">The existing type before which to insert.</typeparam>
		/// <typeparam name="T">The type to insert.</typeparam>
		/// <exception cref="InvalidOperationException">the resolver does not support inserting types, or 
		/// one of the types is not a valid type for the resolver, or the existing type is not in the collection,
		/// or the new type is already in the collection of types.</exception>
		public void InsertTypeBefore<Texisting, T>()
		{
			InsertTypeBefore(typeof(Texisting), typeof(T));
		}

		/// <summary>
		/// Returns a value indicating whether the specified type is already in the collection of types.
		/// </summary>
		/// <param name="value">The type to look for.</param>
		/// <returns>A value indicating whether the type is already in the collection of types.</returns>
		public virtual bool ContainsType(Type value)
		{
			using (new ReadLock(_lock))
			{
				return _instanceTypes.Contains(value);
			}
		}

		/// <summary>
		/// Returns a value indicating whether the specified type is already in the collection of types.
		/// </summary>
		/// <typeparam name="T">The type to look for.</typeparam>
		/// <returns>A value indicating whether the type is already in the collection of types.</returns>
		public bool ContainsType<T>()
		{
			return ContainsType(typeof(T));
		}

		#endregion

		/// <summary>
		/// Returns a WriteLock to use when modifying collections
		/// </summary>
		/// <returns></returns>
		protected WriteLock GetWriteLock()
		{
			return new WriteLock(_lock);
		}
		
		/// <summary>
		/// Throws an exception if resolution is frozen
		/// </summary>
		protected void EnsureResolutionNotFrozen()
		{
			if (Resolution.IsFrozen)
				throw new InvalidOperationException("The type list cannot be modified after resolution has been frozen");
		}

		/// <summary>
		/// Throws an exception if this does not support Remove
		/// </summary>
		protected void EnsureRemoveSupport()
		{
			if (!SupportsRemove)
				throw new InvalidOperationException("This resolver does not support Removing types");
		}

		/// <summary>
		/// Throws an exception if this does not support Clear
		/// </summary>
		protected void EnsureClearSupport()
		{
			if (!SupportsClear)
				throw new InvalidOperationException("This resolver does not support Clearing types");
		}

		/// <summary>
		/// Throws an exception if this does not support Add
		/// </summary>
		protected void EnsureAddSupport()
		{
			if (!SupportsAdd)
				throw new InvalidOperationException("This resolver does not support Adding new types");
		}

		/// <summary>
		/// Throws an exception if this does not support insert
		/// </summary>
		protected void EnsureInsertSupport()
		{
			if (!SupportsInsert)
				throw new InvalidOperationException("This resolver does not support Inserting new types");
		}

		protected virtual bool SupportsAdd
		{
			get { return true; }
		}

		protected virtual bool SupportsInsert
		{
			get { return true; }
		}

		protected virtual bool SupportsClear
		{
			get { return true; }
		}

		protected virtual bool SupportsRemove
		{
			get { return true; }
		}
	}
}