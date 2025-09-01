// JavaScript functions for PoFastType with enhanced debugging

window.focusElement = function (element) {
    if (element) {
        // Add a small delay to ensure the element is ready
        setTimeout(function() {
            element.focus();
        }, 10); // 10ms delay
    }
};

// Debug logging system
window.PoFastTypeDebug = {
    logs: [],
    networkActivity: [],
    
    // Log browser console messages
    initConsoleLogging: function() {
        const originalLog = console.log;
        const originalError = console.error;
        const originalWarn = console.warn;
        const originalInfo = console.info;
        
        console.log = function(...args) {
            PoFastTypeDebug.captureLog('log', args);
            originalLog.apply(console, args);
        };
        
        console.error = function(...args) {
            PoFastTypeDebug.captureLog('error', args);
            originalError.apply(console, args);
        };
        
        console.warn = function(...args) {
            PoFastTypeDebug.captureLog('warn', args);
            originalWarn.apply(console, args);
        };
        
        console.info = function(...args) {
            PoFastTypeDebug.captureLog('info', args);
            originalInfo.apply(console, args);
        };
        
        // Capture unhandled errors
        window.addEventListener('error', function(event) {
            PoFastTypeDebug.captureLog('error', [`Unhandled Error: ${event.message}`, `File: ${event.filename}`, `Line: ${event.lineno}`, `Column: ${event.colno}`, event.error]);
        });
        
        // Capture unhandled promise rejections
        window.addEventListener('unhandledrejection', function(event) {
            PoFastTypeDebug.captureLog('error', [`Unhandled Promise Rejection: ${event.reason}`]);
        });
    },
    
    // Capture log messages
    captureLog: function(level, args) {
        const logEntry = {
            timestamp: new Date().toISOString(),
            level: level,
            message: args.map(arg => typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)).join(' '),
            url: window.location.href,
            userAgent: navigator.userAgent
        };
        
        this.logs.push(logEntry);
        
        // Send to server if enabled
        this.sendLogToServer(logEntry);
    },
    
    // Monitor network activity
    initNetworkMonitoring: function() {
        // Override fetch
        const originalFetch = window.fetch;
        window.fetch = function(...args) {
            const startTime = Date.now();
            const url = args[0];
            const options = args[1] || {};
            
            PoFastTypeDebug.captureNetworkActivity('fetch', url, 'outgoing', options);
            
            return originalFetch.apply(this, args).then(response => {
                const endTime = Date.now();
                PoFastTypeDebug.captureNetworkActivity('fetch', url, 'response', {
                    status: response.status,
                    statusText: response.statusText,
                    duration: endTime - startTime
                });
                return response;
            }).catch(error => {
                const endTime = Date.now();
                PoFastTypeDebug.captureNetworkActivity('fetch', url, 'error', {
                    error: error.message,
                    duration: endTime - startTime
                });
                throw error;
            });
        };
        
        // Override XMLHttpRequest
        const originalXHROpen = XMLHttpRequest.prototype.open;
        const originalXHRSend = XMLHttpRequest.prototype.send;
        
        XMLHttpRequest.prototype.open = function(method, url, ...args) {
            this._debugInfo = { method, url, startTime: Date.now() };
            return originalXHROpen.apply(this, [method, url, ...args]);
        };
        
        XMLHttpRequest.prototype.send = function(data) {
            if (this._debugInfo) {
                PoFastTypeDebug.captureNetworkActivity('xhr', this._debugInfo.url, 'outgoing', {
                    method: this._debugInfo.method,
                    data: data
                });
                
                this.addEventListener('load', () => {
                    PoFastTypeDebug.captureNetworkActivity('xhr', this._debugInfo.url, 'response', {
                        status: this.status,
                        statusText: this.statusText,
                        duration: Date.now() - this._debugInfo.startTime
                    });
                });
                
                this.addEventListener('error', () => {
                    PoFastTypeDebug.captureNetworkActivity('xhr', this._debugInfo.url, 'error', {
                        duration: Date.now() - this._debugInfo.startTime
                    });
                });
            }
            return originalXHRSend.apply(this, arguments);
        };
    },
    
    // Capture network activity
    captureNetworkActivity: function(type, url, direction, details) {
        const activity = {
            timestamp: new Date().toISOString(),
            type: type,
            url: url,
            direction: direction,
            details: details
        };
        
        this.networkActivity.push(activity);
        
        // Send to server if enabled
        this.sendNetworkActivityToServer(activity);
    },
    
    // Send log to server
    sendLogToServer: function(logEntry) {
        try {
            // Use the original fetch to avoid infinite loops
            const originalFetch = window.fetch;
            originalFetch('/api/debug/log', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(logEntry)
            }).catch(() => {
                // Silently fail to avoid debug logging errors
            });
        } catch (e) {
            // Silently fail
        }
    },
    
    // Send network activity to server
    sendNetworkActivityToServer: function(activity) {
        try {
            // Use the original fetch to avoid infinite loops
            const originalFetch = window.fetch;
            originalFetch('/api/debug/network', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(activity)
            }).catch(() => {
                // Silently fail to avoid debug logging errors
            });
        } catch (e) {
            // Silently fail
        }
    },
    
    // Get all captured logs
    getAllLogs: function() {
        return this.logs;
    },
    
    // Get all network activity
    getAllNetworkActivity: function() {
        return this.networkActivity;
    },
    
    // Clear logs
    clearLogs: function() {
        this.logs = [];
        this.networkActivity = [];
    },
    
    // Export logs to downloadable file
    exportLogs: function() {
        const data = {
            timestamp: new Date().toISOString(),
            logs: this.logs,
            networkActivity: this.networkActivity,
            userAgent: navigator.userAgent,
            url: window.location.href
        };
        
        const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `pofasttype-debug-${new Date().toISOString().split('T')[0]}.json`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};

// Initialize debugging when page loads
document.addEventListener('DOMContentLoaded', function() {
    PoFastTypeDebug.initConsoleLogging();
    PoFastTypeDebug.initNetworkMonitoring();
    console.log('PoFastType Debug System Initialized');
});

// Make debugging available globally
window.exportDebugLogs = PoFastTypeDebug.exportLogs;
