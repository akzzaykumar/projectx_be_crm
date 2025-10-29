# 🌍 Live Location Tracking - Complete Implementation Guide

## Overview

This guide explains how to implement real-time GPS location tracking in the FunBookr CRM system for distance-based features like finding nearby activities, providers, and locations.

---

## 🎯 What's Been Implemented

### **Backend API Endpoint**
✅ `GET /api/Locations/nearby` - Find locations near user's GPS coordinates

**Features:**
- Haversine formula for accurate distance calculations
- Configurable search radius (default 50km)
- Returns results sorted by distance (nearest first)
- Includes activity and provider counts
- Validates GPS coordinates

---

## 📱 Frontend Implementation

### **1. Web Browser (JavaScript/TypeScript)**

```javascript
// Get user's current location
async function getUserLocation() {
  return new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
      reject(new Error('Geolocation not supported'));
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        resolve({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracy: position.coords.accuracy
        });
      },
      (error) => reject(error),
      {
        enableHighAccuracy: true,  // Use GPS if available
        timeout: 10000,             // 10 second timeout
        maximumAge: 300000          // Accept cached position up to 5 minutes old
      }
    );
  });
}

// Find nearby locations
async function findNearbyLocations(radiusKm = 50) {
  try {
    // Get user's location
    const userLocation = await getUserLocation();
    console.log('User location:', userLocation);

    // Call backend API
    const response = await fetch(
      `https://api.funbookr.com/api/Locations/nearby?` +
      `latitude=${userLocation.latitude}&` +
      `longitude=${userLocation.longitude}&` +
      `radiusKm=${radiusKm}&` +
      `maxResults=10`
    );

    const result = await response.json();

    if (result.success) {
      console.log('Nearby locations:', result.data);
      displayLocations(result.data);
    } else {
      console.error('Error:', result.message);
    }
  } catch (error) {
    console.error('Failed to get location:', error);
    // Fallback: Show all locations or ask user to select manually
    showLocationSelector();
  }
}

// Display locations to user
function displayLocations(locations) {
  locations.forEach(location => {
    console.log(`${location.name} - ${location.distanceKm} km away`);
    console.log(`${location.totalActivities} activities available`);
  });
}
```

### **2. React Example**

```typescript
import { useState, useEffect } from 'react';

interface UserLocation {
  latitude: number;
  longitude: number;
  accuracy: number;
}

interface NearbyLocation {
  locationId: string;
  name: string;
  city: string;
  distanceKm: number;
  totalActivities: number;
  totalProviders: number;
}

export function useUserLocation() {
  const [location, setLocation] = useState<UserLocation | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!navigator.geolocation) {
      setError('Geolocation not supported');
      setLoading(false);
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLocation({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracy: position.coords.accuracy,
        });
        setLoading(false);
      },
      (err) => {
        setError(err.message);
        setLoading(false);
      },
      { enableHighAccuracy: true }
    );
  }, []);

  return { location, error, loading };
}

export function NearbyLocationsComponent() {
  const { location, error, loading } = useUserLocation();
  const [nearbyLocations, setNearbyLocations] = useState<NearbyLocation[]>([]);

  useEffect(() => {
    if (location) {
      fetchNearbyLocations(location);
    }
  }, [location]);

  const fetchNearbyLocations = async (userLoc: UserLocation) => {
    try {
      const response = await fetch(
        `/api/Locations/nearby?` +
        `latitude=${userLoc.latitude}&` +
        `longitude=${userLoc.longitude}&` +
        `radiusKm=50&maxResults=10`
      );
      const result = await response.json();
      
      if (result.success) {
        setNearbyLocations(result.data);
      }
    } catch (err) {
      console.error('Failed to fetch nearby locations:', err);
    }
  };

  if (loading) return <div>Getting your location...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <h2>Locations Near You</h2>
      {nearbyLocations.map((loc) => (
        <div key={loc.locationId} className="location-card">
          <h3>{loc.name}</h3>
          <p>{loc.city}</p>
          <p>{loc.distanceKm} km away</p>
          <p>{loc.totalActivities} activities</p>
        </div>
      ))}
    </div>
  );
}
```

### **3. Mobile App (React Native)**

```typescript
import Geolocation from '@react-native-community/geolocation';

// Request location permission (iOS/Android)
import { PermissionsAndroid, Platform } from 'react-native';

async function requestLocationPermission() {
  if (Platform.OS === 'android') {
    const granted = await PermissionsAndroid.request(
      PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION,
      {
        title: 'Location Permission',
        message: 'FunBookr needs access to your location to show nearby activities',
        buttonPositive: 'OK',
      }
    );
    return granted === PermissionsAndroid.RESULTS.GRANTED;
  }
  return true; // iOS handles permissions automatically
}

// Get current location
async function getCurrentLocation(): Promise<UserLocation> {
  const hasPermission = await requestLocationPermission();
  
  if (!hasPermission) {
    throw new Error('Location permission denied');
  }

  return new Promise((resolve, reject) => {
    Geolocation.getCurrentPosition(
      (position) => {
        resolve({
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
          accuracy: position.coords.accuracy,
        });
      },
      (error) => reject(error),
      { enableHighAccuracy: true, timeout: 15000, maximumAge: 10000 }
    );
  });
}

// Watch location changes (for real-time tracking)
function watchUserLocation(callback: (location: UserLocation) => void) {
  return Geolocation.watchPosition(
    (position) => {
      callback({
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
        accuracy: position.coords.accuracy,
      });
    },
    (error) => console.error(error),
    { enableHighAccuracy: true, distanceFilter: 100 } // Update every 100 meters
  );
}
```

---

## 🔧 API Usage Examples

### **Basic Request**
```http
GET /api/Locations/nearby?latitude=19.0760&longitude=72.8777&radiusKm=50
```

### **Response**
```json
{
  "success": true,
  "data": [
    {
      "locationId": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Mumbai",
      "city": "Mumbai",
      "state": "Maharashtra",
      "country": "India",
      "latitude": 19.0760,
      "longitude": 72.8777,
      "distanceKm": 0.0,
      "totalActivities": 120,
      "totalProviders": 45
    },
    {
      "locationId": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Pune",
      "city": "Pune",
      "state": "Maharashtra",
      "country": "India",
      "latitude": 18.5204,
      "longitude": 73.8567,
      "distanceKm": 118.32,
      "totalActivities": 85,
      "totalProviders": 32
    }
  ]
}
```

### **With Custom Radius**
```http
GET /api/Locations/nearby?latitude=15.2993&longitude=74.1240&radiusKm=100&maxResults=5
```

---

## 🎨 Use Case Implementations

### **1. Distance-Based Activity Search**

```typescript
// Find activities near user
async function findNearbyActivities(userLat: number, userLon: number) {
  // Step 1: Get nearby locations
  const locationsResponse = await fetch(
    `/api/Locations/nearby?latitude=${userLat}&longitude=${userLon}&radiusKm=50`
  );
  const locationsResult = await locationsResponse.json();
  
  if (!locationsResult.success) return [];

  // Step 2: Get activities from nearby locations
  const locationIds = locationsResult.data.map(loc => loc.locationId);
  
  const activitiesPromises = locationIds.map(async (locationId) => {
    const response = await fetch(`/api/Activities?locationId=${locationId}`);
    return response.json();
  });

  const activitiesResults = await Promise.all(activitiesPromises);
  
  // Combine and sort by location distance
  const allActivities = activitiesResults
    .filter(r => r.success)
    .flatMap(r => r.data.items);
    
  return allActivities;
}
```

### **2. Location-Based Recommendations**

```typescript
// Show personalized recommendations based on location
async function getLocationBasedRecommendations(userId: string) {
  const userLocation = await getUserLocation();
  
  const nearbyLocations = await fetch(
    `/api/Locations/nearby?` +
    `latitude=${userLocation.latitude}&` +
    `longitude=${userLocation.longitude}&` +
    `radiusKm=25`
  ).then(r => r.json());

  // Get featured activities from nearest location
  const nearestLocation = nearbyLocations.data[0];
  
  const activities = await fetch(
    `/api/Activities?locationId=${nearestLocation.locationId}&featured=true`
  ).then(r => r.json());

  return {
    location: nearestLocation,
    activities: activities.data.items,
    message: `${activities.data.totalCount} activities ${nearestLocation.distanceKm}km away`
  };
}
```

### **3. Check-In Verification**

```typescript
// Verify customer is at booking location
async function verifyCheckIn(bookingId: string, userLat: number, userLon: number) {
  // Get booking details
  const booking = await fetch(`/api/Bookings/${bookingId}`).then(r => r.json());
  
  const activityLocation = booking.data.activity.location;
  
  // Calculate distance
  const distance = calculateDistance(
    userLat, userLon,
    activityLocation.latitude, activityLocation.longitude
  );

  const maxDistanceKm = 0.5; // 500 meters tolerance
  
  if (distance <= maxDistanceKm) {
    // Allow check-in
    await fetch(`/api/Bookings/${bookingId}/checkin`, { method: 'PUT' });
    return { success: true, message: 'Check-in successful' };
  } else {
    return { 
      success: false, 
      message: `You're ${distance.toFixed(2)}km away. Please be within ${maxDistanceKm}km to check in.`
    };
  }
}
```

---

## 🔐 Privacy & Permissions

### **Browser Permissions**
```javascript
// Check if location permission is granted
async function checkLocationPermission() {
  if (!navigator.permissions) {
    return 'unknown';
  }

  const result = await navigator.permissions.query({ name: 'geolocation' });
  return result.state; // 'granted', 'denied', or 'prompt'
}

// Request permission with user-friendly message
async function requestLocation() {
  const status = await checkLocationPermission();
  
  if (status === 'denied') {
    alert('Location access denied. Please enable it in browser settings.');
    return null;
  }

  try {
    return await getUserLocation();
  } catch (error) {
    console.error('Location error:', error);
    return null;
  }
}
```

### **Best Practices**
1. ✅ Always explain WHY you need location access
2. ✅ Provide fallback option (manual location selection)
3. ✅ Cache location to avoid repeated requests
4. ✅ Use `maximumAge` to accept cached positions
5. ✅ Handle errors gracefully
6. ✅ Don't track location unless necessary

---

## 📊 Database Considerations

### **Current Schema**
Your `Location` entity already has GPS coordinates:
```csharp
public decimal? Latitude { get; private set; }
public decimal? Longitude { get; private set; }
```

### **For Real-Time User Tracking (Optional)**
If you want to store user location history:

```sql
CREATE TABLE user_locations (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8),
    accuracy DECIMAL(10, 2),
    recorded_at TIMESTAMP,
    location_type VARCHAR(50), -- 'manual', 'gps', 'ip_lookup'
    INDEX idx_user_recorded (user_id, recorded_at)
);
```

---

## 🚀 Advanced Features

### **1. IP-Based Location Fallback**
```csharp
// If GPS fails, use IP geolocation as fallback
public async Task<(decimal Lat, decimal Lon)?> GetLocationFromIP(string ipAddress)
{
    // Use service like ipapi.co, ipstack, or ip2location
    var client = new HttpClient();
    var response = await client.GetAsync($"https://ipapi.co/{ipAddress}/json/");
    var data = await response.Content.ReadFromJsonAsync<IpLocationResponse>();
    
    return (data.Latitude, data.Longitude);
}
```

### **2. Location Caching**
```typescript
// Cache location in localStorage
const LOCATION_CACHE_KEY = 'user_location';
const CACHE_DURATION = 30 * 60 * 1000; // 30 minutes

function getCachedLocation(): UserLocation | null {
  const cached = localStorage.getItem(LOCATION_CACHE_KEY);
  if (!cached) return null;

  const { location, timestamp } = JSON.parse(cached);
  
  if (Date.now() - timestamp > CACHE_DURATION) {
    return null; // Cache expired
  }

  return location;
}

function setCachedLocation(location: UserLocation) {
  localStorage.setItem(LOCATION_CACHE_KEY, JSON.stringify({
    location,
    timestamp: Date.now()
  }));
}
```

### **3. Background Location Updates**
```typescript
// Update location periodically
let locationWatchId: number;

function startLocationTracking() {
  locationWatchId = navigator.geolocation.watchPosition(
    (position) => {
      const location = {
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
        accuracy: position.coords.accuracy
      };
      
      // Send to backend or update UI
      updateNearbyActivities(location);
    },
    (error) => console.error(error),
    { 
      enableHighAccuracy: false, // Save battery
      maximumAge: 600000,        // Accept 10-minute-old positions
      timeout: 5000
    }
  );
}

function stopLocationTracking() {
  if (locationWatchId) {
    navigator.geolocation.clearWatch(locationWatchId);
  }
}
```

---

## 🧪 Testing

### **Test with Swagger**
```
1. Open Swagger UI: https://localhost:5001/swagger
2. Find: GET /api/Locations/nearby
3. Test coordinates:
   - Mumbai: 19.0760, 72.8777
   - Goa: 15.2993, 74.1240
   - Delhi: 28.6139, 77.2090
```

### **Browser Console Test**
```javascript
navigator.geolocation.getCurrentPosition(
  pos => console.log('Lat:', pos.coords.latitude, 'Lon:', pos.coords.longitude)
);
```

---

## 📱 Mobile Testing Tools

- **Android**: Enable "Mock Locations" in Developer Options
- **iOS**: Simulator > Debug > Location > Custom Location
- **Browser**: DevTools > Sensors > Location Override (Chrome/Edge)

---

## ⚡ Performance Tips

1. **Batch Requests**: Get locations and activities in single request
2. **Pagination**: Limit results with `maxResults` parameter
3. **Caching**: Cache location for 15-30 minutes
4. **Debouncing**: Don't update on every location change
5. **Lazy Loading**: Load activities only when user scrolls

---

## 🎯 Summary

**What You Have Now:**
✅ Backend API endpoint for nearby location search
✅ Haversine distance calculation
✅ Distance-sorted results with activity counts
✅ Configurable search radius

**What You Need to Add (Frontend):**
1. JavaScript/React code to get user's GPS location
2. API calls to `/api/Locations/nearby`
3. UI to display nearby locations and activities
4. Permission handling and error messages
5. Fallback to manual location selection

**Next Steps:**
1. Test the API endpoint in Swagger
2. Implement frontend location capture
3. Build UI for nearby activities
4. Add location-based filters to activity search
5. Consider adding user location history (optional)

Ready to implement! 🚀
