using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;

namespace Dynamics365.Security.Services
{
    /// <summary>
    /// Provides a connected <see cref="IOrganizationService"/> for the Dynamics 365
    /// environment, using the CRM SDK's <see cref="CrmServiceClient"/>.
    /// </summary>
    public sealed class CrmConnectionService : IDisposable
    {
        private CrmServiceClient _serviceClient;
        private bool _disposed;

        // ------------------------------------------------------------------ //
        //  Construction
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Initialises the service using a CRM SDK connection string.
        /// </summary>
        /// <remarks>
        /// <para>Example OAuth connection string (online):</para>
        /// <code>
        /// AuthType=OAuth;
        /// Url=https://yourorg.crm.dynamics.com;
        /// AppId=&lt;your-app-id&gt;;
        /// RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;
        /// LoginPrompt=Auto
        /// </code>
        /// <para>Example user/password connection string (online):</para>
        /// <code>
        /// AuthType=Office365;
        /// Username=user@yourtenant.onmicrosoft.com;
        /// Password=yourPassword;
        /// Url=https://yourorg.crm.dynamics.com
        /// </code>
        /// <para>Example on-premises connection string:</para>
        /// <code>
        /// AuthType=AD;
        /// Url=http://yourserver/yourorg;
        /// Domain=YOURDOMAIN;
        /// Username=yourUser;
        /// Password=yourPassword
        /// </code>
        /// </remarks>
        /// <param name="connectionString">CRM SDK connection string.</param>
        /// <exception cref="ArgumentNullException">
        ///   Thrown when <paramref name="connectionString"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   Thrown when the connection could not be established.
        /// </exception>
        public CrmConnectionService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            Connect(connectionString);
        }

        // ------------------------------------------------------------------ //
        //  Public API
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Returns the connected <see cref="IOrganizationService"/> for use with
        /// CRM SDK query and execute methods.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///   Thrown when the service has already been disposed.
        /// </exception>
        public IOrganizationService OrganizationService
        {
            get
            {
                ThrowIfDisposed();
                return _serviceClient;
            }
        }

        /// <summary>
        /// Gets the friendly name of the connected organisation.
        /// </summary>
        public string OrganizationFriendlyName
        {
            get
            {
                ThrowIfDisposed();
                return _serviceClient.ConnectedOrgFriendlyName;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the service client is currently connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                ThrowIfDisposed();
                return _serviceClient.IsReady;
            }
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        private void Connect(string connectionString)
        {
            _serviceClient = new CrmServiceClient(connectionString);

            if (!_serviceClient.IsReady)
            {
                string error = _serviceClient.LastCrmError
                    ?? "Unknown error – check the connection string and credentials.";
                throw new InvalidOperationException(
                    $"Unable to connect to Dynamics 365: {error}");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CrmConnectionService));
        }

        // ------------------------------------------------------------------ //
        //  IDisposable
        // ------------------------------------------------------------------ //

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _serviceClient?.Dispose();
                _disposed = true;
            }
        }
    }
}
