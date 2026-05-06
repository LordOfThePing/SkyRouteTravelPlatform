export interface PassengerForm {
  fullName: string;
  email: string;
  documentType: 'Passport' | 'NationalId';
  documentNumber: string;
}

export interface BookingRequest {
  flightId: string;
  originCode: string;
  destinationCode: string;
  cabinClass: string;
  totalPrice: number;
  passengers: PassengerForm[];
}

export interface BookingResponse {
  bookingReference: string;
  createdAt: string;
  passengerCount: number;
}
