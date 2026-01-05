# Li'nage Troubleshooting Guide

## Error: "Exception has been thrown by the target of an invocation"

This error typically occurs when the application fails to initialize the database connection.

### Quick Checklist

#### 1. **SQL Server Status**
- [ ] Is SQL Server running?
  - **LocalDB**: Run `sqllocaldb start` in PowerShell (Admin)
  - **Express**: Check Windows Services (sqlservr.exe)
  - **Full Edition**: Check Windows Services

#### 2. **Connection String**
- [ ] Open `App.config` in the Li'nage project root
- [ ] Verify the `LinageDbContext` connection string:
  ```xml
  <connectionStrings>
    <add name="LinageDbContext" 
         connectionString="Data Source=(localdb)\mssqllocaldb;Initial Catalog=LinageDb;Integrated Security=true;" 
         providerName="System.Data.SqlClient" />
  </connectionStrings>
  ```
- [ ] Common connection strings:
  - **LocalDB** (recommended for dev): `Data Source=(localdb)\mssqllocaldb;Initial Catalog=LinageDb;Integrated Security=true;`
  - **SQL Express**: `Data Source=.\SQLEXPRESS;Initial Catalog=LinageDb;Integrated Security=true;`
  - **Custom Server**: `Data Source=YOUR_SERVER;Initial Catalog=LinageDb;Integrated Security=true;`

#### 3. **Database Access**
- [ ] Test connection with SQL Server Management Studio (SSMS)
- [ ] Ensure your Windows user account has SQL Server access
- [ ] If using SQL authentication, update connection string with username/password

#### 4. **Entity Framework Migrations**
- [ ] The app should auto-create the database on first run
- [ ] If database creation fails, check the logs folder: `logs/`
- [ ] Clear the database and let it recreate:
  1. Drop the `LinageDb` database if it exists
  2. Run the application again
  3. Entity Framework will auto-create with latest schema

#### 5. **Check Logs**
- [ ] Review error logs in the `logs/` folder
- [ ] Look for details about database connection failures
- [ ] Share these logs if reporting a bug

### Diagnostic Steps

1. **Verify SQL Server is accessible**:
   ```powershell
   # Test LocalDB
   sqllocaldb info mssqllocaldb
   ```

2. **Check connection string in App.config**:
   - Line 6-12 (approximate location)
   - Ensure "LinageDbContext" matches exactly (case-sensitive)

3. **Review application logs**:
   - Check `logs/` folder for detailed error messages
   - Look for database connection or migration errors

4. **Rebuild the database**:
   - Delete the `LinageDb` database from SQL Server
   - Run the application again
   - It will auto-create with latest schema

### If Still Failing

After improving error messages, you should now see:
- The actual database error message
- Suggestions about SQL Server configuration
- Technical details about what failed

**Next steps**:
1. Note the exact error message shown
2. Verify SQL Server is running
3. Test the connection string in SSMS
4. Check the logs folder for details
5. Ensure database file permissions are correct

---

## Related Files
- **App.config**: Database connection string
- **Infrastructure/LiNageDbContext.cs**: Database context (lines 22-27)
- **Infrastructure/Migrations/**: Auto-generated migration files
- **logs/**: Application diagnostic logs

## Performance Tips
- Run SQL Server LocalDB for development (lighter weight)
- Backup your `LinageDb` database before major updates
- Enable database logging in DebugLogger if connection issues persist
