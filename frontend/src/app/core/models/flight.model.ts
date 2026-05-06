export interface FlightOffer {
  id: string;
  provider: string;
  flightNumber: string;
  originCode: string;
  destinationCode: string;
  departureTime: string;
  arrivalTime: string;
  durationMinutes: number;
  cabinClass: string;
  pricePerPassenger: number;
  totalPrice: number;
  passengers: number;
  currency: string;
}

export interface SearchRequest {
  originCode: string;
  destinationCode: string;
  departureDate: string;
  passengers: number;
  cabinClass: string;
}

export interface SearchResponse {
  results: FlightOffer[];
}
