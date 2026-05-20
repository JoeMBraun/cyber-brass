Title: Live Content

Description: Fetched live

Source: https://raw.githubusercontent.com/praeclarum/sqlite-net/master/src/SQLite.cs

---

//
// Copyright (c) 2009-2024 Krueger Systems, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
#if WINDOWS_PHONE && !USE_WP8_NATIVE_SQLITE
#define USE_CSHARP_SQLITE
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NET8_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
#if !USE_SQLITEPCL_RAW
using System.Runtime.InteropServices;
#endif
using System.Text;
using System.Threading;

#if USE_CSHARP_SQLITE
using Sqlite3 = Community.CsharpSqlite.Sqlite3;
using Sqlite3DatabaseHandle = Community.CsharpSqlite.Sqlite3.sqlite3;
using Sqlite3Statement = Community.CsharpSqlite.Sqlite3.Vdbe;
#elif USE_WP8_NATIVE_SQLITE
using Sqlite3 = Sqlite.Sqlite3;
using Sqlite3DatabaseHandle = Sqlite.Database;
using Sqlite3Statement = Sqlite.Statement;
#elif USE_SQLITEPCL_RAW
using Sqlite3DatabaseHandle = SQLitePCL.sqlite3;
using Sqlite3BackupHandle = SQLitePCL.sqlite3_backup;
using Sqlite3Statement = SQLitePCL.sqlite3_stmt;
using Sqlite3 = SQLitePCL.raw;
#else
using Sqlite3DatabaseHandle = System.IntPtr;
using Sqlite3BackupHandle = System.IntPtr;
using Sqlite3Statement = System.IntPtr;
#endif

#pragma warning disable 1591 // XML Doc Comments

namespace SQLite
{
	public class SQLiteException : Exception
	{
		public SQLite3.Result Result { get; private set; }

		protected SQLiteException (SQLite3.Result r, string message) : base (message)
		{
			Result = r;
		}

		public static SQLiteException New (SQLite3.Result r, string message)
		{
			return new SQLiteException (r, message);
		}
	}

	public class NotNullConstraintViolationException : SQLiteException
	{
		public IEnumerable<TableMapping.Column> Columns { get; protected set; }

		protected NotNullConstraintViolationException (SQLite3.Result r, string message)
			: this (r, message, null, null)
		{

		}

		protected NotNullConstraintViolationException (SQLite3.Result r, string message, TableMapping mapping, object obj)
			: base (r, message)
		{
			if (mapping != null && obj != null) {
				this.Columns = from c in mapping.Columns
							   where c.IsNullable == false && c.GetValue (obj) == null
							   select c;
			}
		}

		public static new NotNullConstraintViolationException New (SQLite3.Result r, string message)
		{
			return new NotNullConstraintViolationException (r, message);
		}

		public static NotNullConstraintViolationException New (SQLite3.Result r, string message, TableMapping mapping, object obj)
		{
			return new NotNullConstraintViolationException (r, message, mapping, obj);
		}

		public static NotNullConstraintViolationException New (SQLiteException exception, TableMapping mapping, object obj)
		{
			return new NotNullConstraintViolationException (exception.Result, exception.Message, mapping, obj);
		}
	}

	[Flags]
	public enum SQLiteOpenFlags
	{
		ReadOnly = 1, ReadWrite = 2, Create = 4,
		Uri = 0x40, Memory = 0x80,
		NoMutex = 0x8000, FullMutex = 0x10000,
		SharedCache = 0x20000, PrivateCache = 0x40000,
		ProtectionComplete = 0x00100000,
		ProtectionCompleteUnlessOpen = 0x00200000,
		ProtectionCompleteUntilFirstUserAuthentication = 0x00300000,
		ProtectionNone = 0x00400000
	}

	[Flags]
	public enum CreateFlags
	{
		/// <summary>
		/// Use the default creation options
		/// </summary>
		None = 0x000,
		/// <summary>
		/// Create a primary key index for a property called 'Id' (case-insensitive).
		/// This avoids the need for the [PrimaryKey] attribute.
		/// </summary>
		ImplicitPK = 0x001,
		/// <summary>
		/// Create indices for properties ending in 'Id' (case-insensitive).
		/// </summary>
		ImplicitIndex = 0x002,
		/// <summary>
		/// Create a primary key for a property called 'Id' and
		/// create an indices for properties ending in 'Id' (case-insensitive).
		/// </summary>
		AllImplicit = 0x003,
		/// <summary>
		/// Force the primary key property to be auto incrementing.
		/// This avoids the need for the [AutoIncrement] attribute.
		/// The primary key property on the class should have type int or long.
		/// </summary>
		AutoIncPK = 0x004,
		/// <summary>
		/// Create virtual table using FTS3
		/// </summary>
		FullTextSearch3 = 0x100,
		/// <summary>
		/// Create virtual table using FTS4
		/// </summary>
		FullTextSearch4 = 0x200
	}

	public interface ISQLiteConnection : IDisposable
	{
		Sqlite3DatabaseHandle Handle { get; }
		string DatabasePath { get; }
		int LibVersionNumber { get; }
		bool TimeExecution { get; set; }
		bool Trace { get; set; }
		Action<string> Tracer { get; set; }
		bool StoreDateTimeAsTicks { get; }
		bool StoreTimeSpanAsTicks { get; }
		string DateTimeStringFormat { get; }
		TimeSpan BusyTimeout { get; set; }
		IEnumerable<TableMapping> TableMappings { get; }
		bool IsInTransaction { get; }

		event EventHandler<NotifyTableChangedEventArgs> TableChanged;

		void Backup (string destinationDatabasePath, string databaseName = "main");
		void BeginTransaction ();
		void Close ();
		void Commit ();
		SQLiteCommand CreateCommand (string cmdText, params object[] ps);
		SQLiteCommand CreateCommand (string cmdText, Dictionary<string, object> args);
		int CreateIndex (string indexName, string tableName, string[] columnNames, bool unique = false);
		int CreateIndex (string indexName, string tableName, string columnName, bool unique = false);
		int CreateIndex (string tableName, string columnName, bool unique = false);
		int CreateIndex (string tableName, string[] columnNames, bool unique = false);
		int CreateIndex<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (Expression<Func<T, object>> property, bool unique = false);
		CreateTableResult CreateTable<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (CreateFlags createFlags = CreateFlags.None);
		CreateTableResult CreateTable (
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type ty, CreateFlags createFlags = CreateFlags.None);
		CreateTablesResult CreateTables<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T2> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new();
		CreateTablesResult CreateTables<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T2,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T3> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new();
		CreateTablesResult CreateTables<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T2,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T3,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T4> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new();
		CreateTablesResult CreateTables<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T2,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T3,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T4,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T5> (CreateFlags createFlags = CreateFlags.None)
			where T : new()
			where T2 : new()
			where T3 : new()
			where T4 : new()
			where T5 : new();
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires 'DynamicallyAccessedMemberTypes.All' on each input 'Type' instance.")]
#endif
		CreateTablesResult CreateTables (CreateFlags createFlags = CreateFlags.None, params Type[] types);
		IEnumerable<T> DeferredQuery<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (string query, params object[] args) where T : new();
		IEnumerable<object> DeferredQuery (TableMapping map, string query, params object[] args);
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of 'objectToDelete'.")]
#endif
		int Delete (object objectToDelete);
		int Delete<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (object primaryKey);
		int Delete (object primaryKey, TableMapping map);
		int DeleteAll<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> ();
		int DeleteAll (TableMapping map);
		int DropTable<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> ();
		int DropTable (TableMapping map);
		void EnableLoadExtension (bool enabled);
		void EnableWriteAheadLogging ();
		int Execute (string query, params object[] args);
		T ExecuteScalar<T> (string query, params object[] args);
		T Find<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (object pk) where T : new();
		object Find (object pk, TableMapping map);
		T Find<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (Expression<Func<T, bool>> predicate) where T : new();
		T FindWithQuery<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (string query, params object[] args) where T : new();
		object FindWithQuery (TableMapping map, string query, params object[] args);
		T Get<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (object pk) where T : new();
		object Get (object pk, TableMapping map);
		T Get<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (Expression<Func<T, bool>> predicate) where T : new();
		TableMapping GetMapping (
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type type, CreateFlags createFlags = CreateFlags.None);
		TableMapping GetMapping<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (CreateFlags createFlags = CreateFlags.None);
		List<SQLiteConnection.ColumnInfo> GetTableInfo (string tableName);
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of 'obj'.")]
#endif
		int Insert (object obj);
		int Insert (
			object obj,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type objType);
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of 'obj'.")]
#endif
		int Insert (object obj, string extra);
		int Insert (
			object obj,
			string extra,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type objType);
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of all objects in 'objects'.")]
#endif
		int InsertAll (IEnumerable objects, bool runInTransaction = true);
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of all objects in 'objects'.")]
#endif
		int InsertAll (IEnumerable objects, string extra, bool runInTransaction = true);
		int InsertAll (
			IEnumerable objects,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type objType,
			bool runInTransaction = true);
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of 'obj'.")]
#endif
		int InsertOrReplace (object obj);
		int InsertOrReplace (
			object obj,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type objType);
		List<T> Query<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (string query, params object[] args) where T : new();
		List<object> Query (TableMapping map, string query, params object[] args);
		List<T> QueryScalars<T> (string query, params object[] args);
		void ReKey (string key);
		void ReKey (byte[] key);
		void Release (string savepoint);
		void Rollback ();
		void RollbackTo (string savepoint);
		void RunInTransaction (Action action);
		string SaveTransactionPoint ();
		TableQuery<T> Table<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> () where T : new();
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of 'obj'.")]
#endif
		int Update (object obj);
		int Update (
			object obj,
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type objType);
#if NET8_0_OR_GREATER
		[RequiresUnreferencedCode ("This method requires ''DynamicallyAccessedMemberTypes.All' on the runtime type of all objects in 'objects'.")]
#endif
		int UpdateAll (IEnumerable objects, bool runInTransaction = true);
	}

	/// <summary>
	/// An open connection to a SQLite database.
	/// </summary>
	[Preserve (AllMembers = true)]
	public partial class SQLiteConnection : ISQLiteConnection
	{
		private bool _open;
		private TimeSpan _busyTimeout;
		readonly static Dictionary<string, TableMapping> _mappings = new Dictionary<string, TableMapping> ();
		private System.Diagnostics.Stopwatch _sw;
		private long _elapsedMilliseconds = 0;

		private int _transactionDepth = 0;
		private Random _rand = new Random ();

		public Sqlite3DatabaseHandle Handle { get; private set; }
		static readonly Sqlite3DatabaseHandle NullHandle = default (Sqlite3DatabaseHandle);
		static readonly Sqlite3BackupHandle NullBackupHandle = default (Sqlite3BackupHandle);

		/// <summary>
		/// Gets the database path used by this connection.
		/// </summary>
		public string DatabasePath { get; private set; }

		/// <summary>
		/// Gets the SQLite library version number. 3007014 would be v3.7.14
		/// </summary>
		public int LibVersionNumber { get; private set; }

		/// <summary>
		/// Whether Trace lines should be written that show the execution time of queries.
		/// </summary>
		public bool TimeExecution { get; set; }

		/// <summary>
		/// Whether to write queries to <see cref="Tracer"/> during execution.
		/// </summary>
		public bool Trace { get; set; }

		/// <summary>
		/// The delegate responsible for writing trace lines.
		/// </summary>
		/// <value>The tracer.</value>
		public Action<string> Tracer { get; set; }

		/// <summary>
		/// Whether to store DateTime properties as ticks (true) or strings (false).
		/// </summary>
		public bool StoreDateTimeAsTicks { get; private set; }

		/// <summary>
		/// Whether to store TimeSpan properties as ticks (true) or strings (false).
		/// </summary>
		public bool StoreTimeSpanAsTicks { get; private set; }

		/// <summary>
		/// The format to use when storing DateTime properties as strings. Ignored if StoreDateTimeAsTicks is true.
		/// </summary>
		/// <value>The date time string format.</value>
		public string DateTimeStringFormat { get; private set; }

		/// <summary>
		/// The DateTimeStyles value to use when parsing a DateTime property string.
		/// </summary>
		/// <value>The date time style.</value>
		internal System.Globalization.DateTimeStyles DateTimeStyle { get; private set; }

#if USE_SQLITEPCL_RAW && !NO_SQLITEPCL_RAW_BATTERIES
		static SQLiteConnection ()
		{
			SQLitePCL.Batteries_V2.Init ();
		}
#endif

		/// <summary>
		/// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
		/// </summary>
		/// <param name="databasePath">
		/// Specifies the path to the database file.
		/// </param>
		/// <param name="storeDateTimeAsTicks">
		/// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
		/// absolutely do want to store them as Ticks in all new projects. The value of false is
		/// only here for backwards compatibility. There is a *significant* speed advantage, with no
		/// down sides, when setting storeDateTimeAsTicks = true.
		/// If you use DateTimeOffset properties, it will be always stored as ticks regardingless
		/// the storeDateTimeAsTicks parameter.
		/// </param>
		public SQLiteConnection (string databasePath, bool storeDateTimeAsTicks = true)
			: this (new SQLiteConnectionString (databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create, storeDateTimeAsTicks))
		{
		}

		/// <summary>
		/// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
		/// </summary>
		/// <param name="databasePath">
		/// Specifies the path to the database file.
		/// </param>
		/// <param name="openFlags">
		/// Flags controlling how the connection should be opened.
		/// </param>
		/// <param name="storeDateTimeAsTicks">
		/// Specifies whether to store DateTime properties as ticks (true) or strings (false). You
		/// absolutely do want to store them as Ticks in all new projects. The value of false is
		/// only here for backwards compatibility. There is a *significant* speed advantage, with no
		/// down sides, when setting storeDateTimeAsTicks = true.
		/// If you use DateTimeOffset properties, it will be always stored as ticks regardingless
		/// the storeDateTimeAsTicks parameter.
		/// </param>
		public SQLiteConnection (string databasePath, SQLiteOpenFlags openFlags, bool storeDateTimeAsTicks = true)
			: this (new SQLiteConnectionString (databasePath, openFlags, storeDateTimeAsTicks))
		{
		}

		/// <summary>
		/// Constructs a new SQLiteConnection and opens a SQLite database specified by databasePath.
		/// </summary>
		/// <param name="connectionString">
		/// Details on how to find and open the database.
		/// </param>
		public SQLiteConnection (SQLiteConnectionString connectionString)
		{
			if (connectionString == null)
				throw new ArgumentNullException (nameof (connectionString));
			if (connectionString.DatabasePath == null)
				throw new InvalidOperationException ("DatabasePath must be specified");

			DatabasePath = connectionString.DatabasePath;

			LibVersionNumber = SQLite3.LibVersionNumber ();

#if NETFX_CORE
			SQLite3.SetDirectory(/*temp directory type*/2, Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
#endif

			Sqlite3DatabaseHandle handle;

#if SILVERLIGHT || USE_CSHARP_SQLITE || USE_SQLITEPCL_RAW
			var r = SQLite3.Open (connectionString.DatabasePath, out handle, (int)connectionString.OpenFlags, connectionString.VfsName);
#else
			// open using the byte[]
			// in the case where the path may include Unicode
			// force open to using UTF-8 using sqlite3_open_v2
			var databasePathAsBytes = GetNullTerminatedUtf8 (connectionString.DatabasePath);
			var r = SQLite3.Open (databasePathAsBytes, out handle, (int)connectionString.OpenFlags, connectionString.VfsName);
#endif

			Handle = handle;
			if (r != SQLite3.Result.OK) {
				throw SQLiteException.New (r, String.Format ("Could not open database file: {0} ({1})", DatabasePath, r));
			}
			_open = true;

			StoreDateTimeAsTicks = connectionString.StoreDateTimeAsTicks;
			StoreTimeSpanAsTicks = connectionString.StoreTimeSpanAsTicks;
			DateTimeStringFormat = connectionString.DateTimeStringFormat;
			DateTimeStyle = connectionString.DateTimeStyle;

			BusyTimeout = TimeSpan.FromSeconds (1.0);
			Tracer = line => Debug.WriteLine (line);

			connectionString.PreKeyAction?.Invoke (this);
			if (connectionString.Key is string stringKey) {
				SetKey (stringKey);
			}
			else if (connectionString.Key is byte[] bytesKey) {
				SetKey (bytesKey);
			}
			else if (connectionString.Key != null) {
				throw new InvalidOperationException ("Encryption keys must be strings or byte arrays");
			}
			connectionString.PostKeyAction?.Invoke (this);
		}

		/// <summary>
		/// Enables the write ahead logging. WAL is significantly faster in most scenarios
		/// by providing better concurrency and better disk IO performance than the normal
		/// journal mode. You only need to call this function once in the lifetime of the database.
		/// </summary>
		public void EnableWriteAheadLogging ()
		{
			ExecuteScalar<string> ("PRAGMA journal_mode=WAL");
		}

		/// <summary>
		/// Convert an input string to a quoted SQL string that can be safely used in queries.
		/// </summary>
		/// <returns>The quoted string.</returns>
		/// <param name="unsafeString">The unsafe string to quote.</param>
		static string Quote (string unsafeString)
		{
			// TODO: Doesn't call sqlite3_mprintf("%Q", u) because we're waiting on https://github.com/ericsink/SQLitePCL.raw/issues/153
			if (unsafeString == null)
				return "NULL";
			var safe = unsafeString.Replace ("'", "''");
			return "'" + safe + "'";
		}

		/// <summary>
		/// Sets the key used to encrypt/decrypt the database with "pragma key = ...".
		/// This must be the first thing you call before doing anything else with this connection
		/// if your database is encrypted.
		/// This only has an effect if you are using the SQLCipher nuget package.
		/// </summary>
		/// <param name="key">Encryption key plain text that is converted to the real encryption key using PBKDF2 key derivation</param>
		void SetKey (string key)
		{
			if (key == null)
				throw new ArgumentNullException (nameof (key));
			var q = Quote (key);
			ExecuteScalar<string> ("pragma key = " + q);
		}

		/// <summary>
		/// Sets the key used to encrypt/decrypt the database.
		/// This must be the first thing you call before doing anything else with this connection
		/// if your database is encrypted.
		/// This only has an effect if you are using the SQLCipher nuget package.
		/// </summary>
		/// <param name="key">256-bit (32 byte) encryption key data</param>
		void SetKey (byte[] key)
		{
			if (key == null)
				throw new ArgumentNullException (nameof (key));
			if (key.Length != 32 && key.Length != 48)
				throw new ArgumentException ("Key must be 32 bytes (256-bit) or 48 bytes (384-bit)", nameof (key));
			var s = String.Join ("", key.Select (x => x.ToString ("X2")));
			ExecuteScalar<string> ("pragma key = \"x'" + s + "'\"");
		}

		/// <summary>
		/// Change the encryption key for a SQLCipher database with "pragma rekey = ...".
		/// </summary>
		/// <param name="key">Encryption key plain text that is converted to the real encryption key using PBKDF2 key derivation</param>
		public void ReKey (string key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			var q = Quote(key);
			ExecuteScalar<string>("pragma rekey = " + q);
		}

		/// <summary>
		/// Change the encryption key for a SQLCipher database.
		/// </summary>
		/// <param name="key">256-bit (32 byte) or 384-bit (48 bytes) encryption key data</param>
		public void ReKey (byte[] key)
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (key.Length != 32 && key.Length != 48)
				throw new ArgumentException ("Key must be 32 bytes (256-bit) or 48 bytes (384-bit)", nameof (key));
			var s = String.Join("", key.Select(x => x.ToString("X2")));
			ExecuteScalar<string>("pragma rekey = \"x'" + s + "'\"");
		}

		/// <summary>
		/// Enable or disable extension loading.
		/// </summary>
		public void EnableLoadExtension (bool enabled)
		{
			SQLite3.Result r = SQLite3.EnableLoadExtension (Handle, enabled ? 1 : 0);
			if (r != SQLite3.Result.OK) {
				string msg = SQLite3.GetErrmsg (Handle);
				throw SQLiteException.New (r, msg);
			}
		}

#if !USE_SQLITEPCL_RAW
		static byte[] GetNullTerminatedUtf8 (string s)
		{
			var utf8Length = System.Text.Encoding.UTF8.GetByteCount (s);
			var bytes = new byte [utf8Length + 1];
			utf8Length = System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
			return bytes;
		}
#endif

		/// <summary>
		/// Sets a busy handler to sleep the specified amount of time when a table is locked.
		/// The handler will sleep multiple times until a total time of <see cref="BusyTimeout"/> has accumulated.
		/// </summary>
		public TimeSpan BusyTimeout {
			get { return _busyTimeout; }
			set {
				_busyTimeout = value;
				if (Handle != NullHandle) {
					SQLite3.BusyTimeout (Handle, (int)_busyTimeout.TotalMilliseconds);
				}
			}
		}

		/// <summary>
		/// Returns the mappings from types to tables that the connection
		/// currently understands.
		/// </summary>
		public IEnumerable<TableMapping> TableMappings {
			get {
				lock (_mappings) {
					return new List<TableMapping> (_mappings.Values);
				}
			}
		}

		/// <summary>
		/// Retrieves the mapping that is automatically generated for the given type.
		/// </summary>
		/// <param name="type">
		/// The type whose mapping to the database is returned.
		/// </param>
		/// <param name="createFlags">
		/// Optional flags allowing implicit PK and indexes based on naming conventions
		/// </param>
		/// <returns>
		/// The mapping represents the schema of the columns of the database and contains
		/// methods to set and get properties of objects.
		/// </returns>
		public TableMapping GetMapping (
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type type,
			CreateFlags createFlags = CreateFlags.None)
		{
			TableMapping map;
			var key = type.FullName;
			lock (_mappings) {
				if (_mappings.TryGetValue (key, out map)) {
					if (createFlags != CreateFlags.None && createFlags != map.CreateFlags) {
						map = new TableMapping (type, createFlags);
						_mappings[key] = map;
					}
				}
				else {
					map = new TableMapping (type, createFlags);
					_mappings.Add (key, map);
				}
			}
			return map;
		}

		/// <summary>
		/// Retrieves the mapping that is automatically generated for the given type.
		/// </summary>
		/// <param name="createFlags">
		/// Optional flags allowing implicit PK and indexes based on naming conventions
		/// </param>
		/// <returns>
		/// The mapping represents the schema of the columns of the database and contains
		/// methods to set and get properties of objects.
		/// </returns>
		public TableMapping GetMapping<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (CreateFlags createFlags = CreateFlags.None)
		{
			return GetMapping (typeof (T), createFlags);
		}

		private struct IndexedColumn
		{
			public int Order;
			public string ColumnName;
		}

		private struct IndexInfo
		{
			public string IndexName;
			public string TableName;
			public bool Unique;
			public List<IndexedColumn> Columns;
		}

		/// <summary>
		/// Executes a "drop table" on the database.  This is non-recoverable.
		/// </summary>
		public int DropTable<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> ()
		{
			return DropTable (GetMapping (typeof (T)));
		}

		/// <summary>
		/// Executes a "drop table" on the database.  This is non-recoverable.
		/// </summary>
		/// <param name="map">
		/// The TableMapping used to identify the table.
		/// </param>
		public int DropTable (TableMapping map)
		{
			var query = string.Format ("drop table if exists \"{0}\"", map.TableName);
			return Execute (query);
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <returns>
		/// Whether the table was created or migrated.
		/// </returns>
		public CreateTableResult CreateTable<
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			T> (CreateFlags createFlags = CreateFlags.None)
		{
			return CreateTable (typeof (T), createFlags);
		}

		/// <summary>
		/// Executes a "create table if not exists" on the database. It also
		/// creates any specified indexes on the columns of the table. It uses
		/// a schema automatically generated from the specified type. You can
		/// later access this schema by calling GetMapping.
		/// </summary>
		/// <param name="ty">Type to reflect to a database table.</param>
		/// <param name="createFlags">Optional flags allowing implicit PK and indexes based on naming conventions.</param>
		/// <returns>
		/// Whether the table was created or migrated.
		/// </returns>
		public CreateTableResult CreateTable (
#if NET8_0_OR_GREATER
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
#endif
			Type ty, CreateFlags createFlags = CreateFlags.None)
		{
			var map = GetMapping (ty, createFlags);

			// Present a nice error if no columns specified
			if (map.Columns.Length == 0) {
				throw new Exception (string.Format ("Cannot create a table without columns (does '{0}' have public properties?)", ty.FullName));
			}

			// Check if the table exists
			var result = CreateTableResult.Created;
			var existingCols = GetTableInfo (map.TableName);

			// Create or migrate it
			if (existingCols.Count == 0) {

				// Facilitate virtual tables a.k.a. full-text search.
				bool fts3 = (createFlags & CreateFlags.FullTextSearch3) != 0;
				bool fts4 = (createFlags & CreateFlags.FullTextSearch4) != 0;
				bool fts = fts3 || fts4;
				var @virtual = fts ? "virtual " : string.Empty;
				var @using = fts3 ? "using fts3 " : fts4 ? "using fts4 " : string.Empty;

				// Build query.
				var query = "create " + @virtual + "table if not exists \"" + map.TableName + "\" " + @using + "(\n";
				var decls = map.Columns.Select (p => Orm.SqlDecl (p, StoreDateTimeAsTicks, StoreTimeSpanAsTicks));
				var decl = string.Join (",\n", decls.ToArray ());
				query += decl;
				query += ")";
				if (map.WithoutRowId) {
					query += " without rowid";
				}

				Execute (query);
			}
			else {
				result = CreateTableResult.Migrated;
				MigrateTable (map, existingCols);
			}

			var indexes = new Dictionary<string, IndexInfo> ();
			foreach (var c in map.Columns) {
				foreach (var i in c.Indices) {
					var iname = i.Name ?? map.TableName + "_" + c.Name;
					IndexInfo iinfo;
					if (!indexes.TryGetValue (iname, out iinfo)) {
						iinfo = new IndexInfo {
							IndexName = iname,
							TableName = map.TableName,
							Unique = i.Unique,
							Columns = new List<IndexedColumn> ()
						};
						indexes.Add (iname, iinfo);
					}

					if (i.Unique != iinfo.Unique)
						throw new Exception ("All the columns in an index must have the same value for their Unique property");

					iinfo.Columns.Add (new IndexedCol

