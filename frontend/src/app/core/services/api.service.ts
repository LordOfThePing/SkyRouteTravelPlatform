import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Airport } from '../models/airport.model';
import { SearchRequest, SearchResponse } from '../models/flight.model';
import { BookingRequest, BookingResponse } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getAirports() {
    return this.http.get<Airport[]>(`${this.base}/airports`);
  }

  searchFlights(request: SearchRequest) {
    const params = new HttpParams()
      .set('originCode', request.originCode)
      .set('destinationCode', request.destinationCode)
      .set('departureDate', request.departureDate)
      .set('passengers', request.passengers.toString())
      .set('cabinClass', request.cabinClass);
    return this.http.get<SearchResponse>(`${this.base}/flights/search`, { params });
  }

  createBooking(request: BookingRequest) {
    return this.http.post<BookingResponse>(`${this.base}/bookings`, request);
  }
}
