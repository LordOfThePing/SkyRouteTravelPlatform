import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { FlightOffer } from '../../core/models/flight.model';
import { BookingPageComponent } from './booking-page.component';

describe('BookingPageComponent', () => {
  let fixture: ComponentFixture<BookingPageComponent>;
  let component: BookingPageComponent;
  let router: jasmine.SpyObj<Router>;

  const apiMock = {
    getAirports: jasmine.createSpy('getAirports'),
    createBooking: jasmine.createSpy('createBooking'),
  };

  const baseFlight: FlightOffer = {
    id: 'offer-1',
    provider: 'GlobalAir',
    flightNumber: 'GA123',
    originCode: 'MAD',
    destinationCode: 'BCN',
    departureTime: '2026-05-10T10:00:00Z',
    arrivalTime: '2026-05-10T11:20:00Z',
    durationMinutes: 80,
    cabinClass: 'Economy',
    pricePerPassenger: 90,
    totalPrice: 180,
    passengers: 2,
    currency: 'USD',
  };

  async function setupWithFlight(flight: FlightOffer) {
    history.replaceState({ flight }, '', location.href);

    await TestBed.configureTestingModule({
      imports: [BookingPageComponent],
      providers: [
        provideRouter([]),
        { provide: ApiService, useValue: apiMock },
      ],
    }).compileComponents();

    router = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    spyOn(router, 'navigate').and.resolveTo(true);

    fixture = TestBed.createComponent(BookingPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    history.replaceState({}, '', location.href);
    TestBed.resetTestingModule();
  });

  it('uses National ID label for domestic routes', async () => {
    apiMock.getAirports.and.returnValue(of([
      { code: 'MAD', name: 'Madrid', city: 'Madrid', country: 'Spain' },
      { code: 'BCN', name: 'Barcelona', city: 'Barcelona', country: 'Spain' },
    ]));
    apiMock.createBooking.and.returnValue(of({
      bookingReference: 'SR-ABC123',
      createdAt: '2026-05-06T10:00:00Z',
      passengerCount: 2,
    }));

    await setupWithFlight(baseFlight);

    expect(component.documentLabel()).toBe('National ID');
  });

  it('uses Passport Number label for international routes', async () => {
    apiMock.getAirports.and.returnValue(of([
      { code: 'MAD', name: 'Madrid', city: 'Madrid', country: 'Spain' },
      { code: 'JFK', name: 'John F Kennedy', city: 'New York', country: 'United States' },
    ]));
    apiMock.createBooking.and.returnValue(of({
      bookingReference: 'SR-ABC123',
      createdAt: '2026-05-06T10:00:00Z',
      passengerCount: 2,
    }));

    await setupWithFlight({ ...baseFlight, destinationCode: 'JFK' });

    expect(component.documentLabel()).toBe('Passport Number');
  });

  it('submits NationalId for domestic bookings', async () => {
    apiMock.getAirports.and.returnValue(of([
      { code: 'MAD', name: 'Madrid', city: 'Madrid', country: 'Spain' },
      { code: 'BCN', name: 'Barcelona', city: 'Barcelona', country: 'Spain' },
    ]));
    apiMock.createBooking.and.returnValue(of({
      bookingReference: 'SR-ABC123',
      createdAt: '2026-05-06T10:00:00Z',
      passengerCount: 2,
    }));

    await setupWithFlight(baseFlight);

    component.passengers.at(0).patchValue({
      fullName: 'Jane Doe',
      email: 'jane@example.com',
      documentNumber: '12345678A',
    });
    component.passengers.at(1).patchValue({
      fullName: 'John Doe',
      email: 'john@example.com',
      documentNumber: '87654321B',
    });

    component.onSubmit();

    expect(apiMock.createBooking).toHaveBeenCalled();
    const payload = apiMock.createBooking.calls.mostRecent().args[0];
    expect(payload.passengers[0].documentType).toBe('NationalId');
    expect(payload.passengers[1].documentType).toBe('NationalId');
  });
});
