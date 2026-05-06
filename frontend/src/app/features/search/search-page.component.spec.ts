import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { FlightOffer } from '../../core/models/flight.model';
import { SearchPageComponent } from './search-page.component';

describe('SearchPageComponent', () => {
  let fixture: ComponentFixture<SearchPageComponent>;
  let component: SearchPageComponent;
  let router: jasmine.SpyObj<Router>;

  const apiMock = {
    getAirports: jasmine.createSpy('getAirports').and.returnValue(of([])),
    searchFlights: jasmine.createSpy('searchFlights').and.returnValue(of({ results: [] })),
  };

  const offersFixture: FlightOffer[] = [
    {
      id: '1',
      provider: 'A',
      flightNumber: 'A100',
      originCode: 'MAD',
      destinationCode: 'JFK',
      departureTime: '2026-05-10T10:00:00Z',
      arrivalTime: '2026-05-10T14:00:00Z',
      durationMinutes: 240,
      cabinClass: 'Economy',
      pricePerPassenger: 100,
      totalPrice: 100,
      passengers: 1,
      currency: 'USD',
    },
    {
      id: '2',
      provider: 'B',
      flightNumber: 'B200',
      originCode: 'MAD',
      destinationCode: 'JFK',
      departureTime: '2026-05-10T08:00:00Z',
      arrivalTime: '2026-05-10T12:00:00Z',
      durationMinutes: 180,
      cabinClass: 'Economy',
      pricePerPassenger: 200,
      totalPrice: 200,
      passengers: 1,
      currency: 'USD',
    },
  ];

  beforeEach(async () => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [SearchPageComponent],
      providers: [
        { provide: ApiService, useValue: apiMock },
        { provide: Router, useValue: router },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('disables submit while form is invalid', () => {
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;

    expect(submitButton.disabled).toBeTrue();
  });

  it('sorts offers by total price descending', () => {
    component.offers.set(offersFixture);
    component.onSortChange('price-desc');

    expect(component.sortedOffers().map(o => o.id)).toEqual(['2', '1']);
  });

  it('sorts offers by total price ascending', () => {
    component.offers.set(offersFixture);
    component.onSortChange('price-asc');

    expect(component.sortedOffers().map(o => o.id)).toEqual(['1', '2']);
  });

  it('sorts offers by duration ascending', () => {
    component.offers.set(offersFixture);
    component.onSortChange('duration-asc');

    expect(component.sortedOffers().map(o => o.id)).toEqual(['2', '1']);
  });

  it('sorts offers by departure time ascending', () => {
    component.offers.set(offersFixture);
    component.onSortChange('departure-asc');

    expect(component.sortedOffers().map(o => o.id)).toEqual(['2', '1']);
  });

  it('navigates to booking with selected flight in state', () => {
    const selected = offersFixture[0];

    component.goToBooking(selected);

    expect(router.navigate).toHaveBeenCalledWith(['/booking'], { state: { flight: selected } });
  });
});
